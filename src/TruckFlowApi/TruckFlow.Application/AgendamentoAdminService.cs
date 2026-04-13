using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AgendamentoAdminService : IAgendamentoAdminService
    {
        private readonly IAgendamentoRepositorio _repo;
        private readonly IValidator<AgendamentoAdminCreateDto> _createValidator;
        private readonly IValidator<AgendamentoAdminUpdateDto> _updateValidator;
        private readonly ILocalDescargaRepositorio _descargaRepo;
        private readonly IFornecedorRepositorio _fornecedorRepositorio;
        private readonly IUnidadeEntregaRepositorio _unidadeRepo;
        private readonly IRecebimentoEventoRepositorio _eventoRepo;
        private readonly IPlanejamentoRecebimentoRepositorio _recebimentoRepo;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly ILocalDescargaRepositorio _localRepo;
        public AgendamentoAdminService
           (
            IAgendamentoRepositorio repo,
            IValidator<AgendamentoAdminCreateDto> createValidator,
            IValidator<AgendamentoAdminUpdateDto> updateValidator,
            ILocalDescargaRepositorio descargaRepositorio,
            IFornecedorRepositorio fornecedorRepositorio,
            IUnidadeEntregaRepositorio entregaRepositorio,
            IRecebimentoEventoRepositorio eventoRepo,
            IPlanejamentoRecebimentoRepositorio recebimentoRepositorio,
            IEmpresaRepositorio empresaRepo,
            ICurrentUserService currentUser,
            IProdutoRepositorio produtoRepositorio,
            ILocalDescargaRepositorio localDescargaRepositorio
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _descargaRepo = descargaRepositorio;
            _fornecedorRepositorio = fornecedorRepositorio;
            _unidadeRepo = entregaRepositorio;
            _eventoRepo = eventoRepo;
            _recebimentoRepo = recebimentoRepositorio;
            _empresaRepo = empresaRepo;
            _currentUser = currentUser;
            _produtoRepo = produtoRepositorio;
            _localRepo = localDescargaRepositorio;
        }

        public async Task<AgendamentoAdminResponse> CreateAvulso(
            AgendamentoAdminCreateDto dto,
            CancellationToken token = default
            )
        {
            await _createValidator.ValidateAndThrowAsync(dto, token);

            var empresaId = _currentUser.EmpresaId ?? throw new UnauthorizedAccessException("Usuário sem empresa.");
            var empresa = await _empresaRepo.GetById(empresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            var fornecedor = await _fornecedorRepositorio.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado");

            Produto? produto = null;
            if (dto.ProdutoId.HasValue && dto.ProdutoId.Value != Guid.Empty)
            {
                produto = await _produtoRepo.GetById(dto.ProdutoId.Value, token)
                    ?? throw new NotFoundException("Produto não encontrado.");
            }

            var localDescarga = await _localRepo.GetById(dto.LocalDescargaId, token)
                ?? throw new NotFoundException("Local não encontrado.");

            var unidade = await _unidadeRepo.GetById(localDescarga.UnidadeEntregaId, token)
                ?? throw new NotFoundException("Unidade de entrega não encontrada.");

            var vaga = new Agendamento
            {
                FornecedorId = dto.FornecedorId,
                Fornecedor = fornecedor,
                Produto = produto,
                ProdutoId = produto?.Id,
                TipoCarga = dto.TipoCarga,
                UnidadeEntregaId = localDescarga.UnidadeEntregaId, 
                UnidadeEntrega = unidade,
                DataInicio = dto.DataInicio,
                DataFim = dto.DataFim,
                UsuarioId = dto.MotoristaId,
                NotaFiscalId = dto.NotaFiscalId,
                VolumeCarga = dto.VolumeCarga,
                PlacaVeiculo = dto.PlacaVeiculo,
                TipoVeiculo = dto.TipoVeiculo,
                EmpresaId = empresa.Id,
                Grade = null,
                GradeId = null,

                StatusAgendamento = dto.MotoristaId.HasValue || !string.IsNullOrWhiteSpace(dto.PlacaVeiculo)
                    ? StatusAgendamento.Agendado
                    : StatusAgendamento.Disponivel
            };

            await _repo.AddAgendamento(vaga, token);
            return MapToResponse(vaga);
        }
        public async Task<List<AgendamentoAdminResponse>> GetByFiltros
            (
                AgendamentoFilterDto filtros,
                CancellationToken token = default
            )
        {

            var dataInicio = filtros.DataInicio == default
                ? DateTime.UtcNow.Date
                : DateTime.SpecifyKind(filtros.DataInicio.Date, DateTimeKind.Utc);

            var dataFim = filtros.DataFim == default
                ? dataInicio.AddDays(7).AddDays(1).AddTicks(-1)
                : DateTime.SpecifyKind(
                    filtros.DataFim.Date.AddDays(1).AddTicks(-1),
                    DateTimeKind.Utc
                  );

            if (dataFim < dataInicio)
            {
                throw new BusinessException("A data final deve ser maior que a inicial.");
            }

            return await _repo.GetAdminViewAsync(
                dataInicio,
                dataFim,
                filtros.FornecedorId,
                filtros.UnidadeEntregaId,
                token);
        }
        public async Task<AgendamentoAdminResponse> GetById(
            Guid id,
            CancellationToken token = default)
        {
            var agendamento = await _repo.GetById(id, token)
                   ?? throw new NotFoundException("Agendamento não encontrado");
            
            return MapToResponse(agendamento);
        }
        public async Task RegistrarChegadaAsync(
            Guid agendamentoId,
            CancellationToken token = default
            )
        {
            var agendamento = await _repo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado");

            agendamento.RegistrarChegada();
            await _repo.Update(agendamento, token);
        }

        public async Task FinalizarOperacao(
            Guid agendamentoId,
            CancellationToken token = default)
        {
            var agendamento = await _repo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado");

            agendamento.FinalizarOperacao();
            await _repo.Update(agendamento, token);
        }

        public async Task CancelarAgendamento(
            Guid agendamentoId,
            CancellationToken token = default
            )
        {
            var agendamento = await _repo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado.");

            agendamento.Cancelar();
            await _repo.Update(agendamento, token);
        }
        public async Task<AgendamentoAdminResponse> Update(
            Guid id,
            AgendamentoAdminUpdateDto dto,
            CancellationToken token = default
            )
        {
            await _updateValidator.ValidateAndThrowAsync(dto, token);

            var agendamento = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Agendamento não encontrado");
           
            var novaDoca = await _unidadeRepo.GetById(dto.UnidadeEntregaId, token)
                ?? throw new BusinessException("Doca inválida");

            agendamento.UnidadeEntregaId = dto.UnidadeEntregaId;
            agendamento.DataInicio = dto.DataInicio;
            agendamento.DataFim = dto.DataFim;

            agendamento.UsuarioId = dto.MotoristaId;
            agendamento.NotaFiscalId = dto.NotaFiscalId;

            agendamento.UpdatedAt = DateTime.UtcNow;

            await _repo.Update(agendamento, token);
            return MapToResponse(agendamento);
        }

        public async Task FinalizarAgendamento(
            Guid agendamentoId,
            decimal quantidadeRecebida,
            CancellationToken token = default
            )
        {

            var empresaId = _currentUser.EmpresaId
                ?? throw new BusinessException("Usuário não vinculado a empresa.");

            var agendamento = await _repo.GetByIdWithFornecedor(agendamentoId, token)
                    ?? throw new NotFoundException("Agendamento não encontrado.");

            agendamento.FinalizarOperacao();

            var planejamento = await _recebimentoRepo
                .GetPlanejamentoAtivoPorFornecedor(
                    agendamento.FornecedorId,
                    token)
                ?? throw new BusinessException(
                    "Não existe planejamento ativo para este fornecedor.");

            Guid produtoId;

            if (agendamento.ProdutoId.HasValue)
            {
                produtoId = agendamento.Grade!.ProdutoId;
            }
            else
            {
                throw new BusinessException("Não foi possível identificar o produto deste agendamento.");
            }

            var item = planejamento.ItemPlanejamentos
                .FirstOrDefault(i => i.ProdutoId == produtoId)
             ?? throw new BusinessException(
                 "Nenhum item do planejamento compatível com a carga."
             );

            var evento = new RecebimentoEvento(
                item,
                quantidadeRecebida,
                agendamento.Id,
                "Recebimento via agendamento (admin)",
                empresaId
            );

            await _eventoRepo.AddAsync(evento, token);

            item.RegistrarRecebimento(quantidadeRecebida);
            planejamento.RecalcularStatus();

            await _repo.Update(agendamento, token);
        }
        public async Task Delete(
            Guid id,
            CancellationToken token = default
            )
        {
            var agendamento = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Agendamento não encontrado");
           

            if (agendamento.StatusAgendamento == StatusAgendamento.EmAndamento)
            {
                throw new BusinessException("Não é possível remover um agendamento em andamento.");
            }

            await _repo.Delete(agendamento, token);
        }
        private static AgendamentoAdminResponse MapToResponse(Agendamento agendamento)
        {
            return new AgendamentoAdminResponse
            {
                Id = agendamento.Id,
                FornecedorNome = agendamento.Fornecedor.Nome,
                MotoristaNome = agendamento.Usuario?.Motorista?.NomeReal,
                DataInicio = agendamento.DataInicio,
                DataFim = agendamento.DataFim,
                Produto = agendamento.Grade?.Produto?.Nome ?? agendamento?.Produto?.Nome!,
                PesoCarga = agendamento.VolumeCarga ?? agendamento.NotaFiscal?.PesoBruto ?? 0,
                PlacaVeiculo = agendamento.PlacaVeiculo ?? agendamento.NotaFiscal?.PlacaVeiculo,
                TipoVeiculo = agendamento.TipoVeiculo.ToString(),
                UnidadeEntrega = agendamento.UnidadeEntrega?.Nome,
                CreatedAt = agendamento.CreatedAt,
                Status = agendamento.StatusAgendamento.ToString(),
                UpdatedAt = agendamento.UpdatedAt,
                DeletedAt = agendamento.DeletedAt
            };
        }
    }
}

