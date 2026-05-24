using FluentValidation;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
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
        private readonly IAgendamentoRecebimentoLifecycleService _lifecycle;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly ICurrentUserService _currentUser;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly ILocalDescargaRepositorio _localRepo;
        private readonly ILogger<AgendamentoAdminService> _logger;
        public AgendamentoAdminService
           (
            IAgendamentoRepositorio repo,
            IValidator<AgendamentoAdminCreateDto> createValidator,
            IValidator<AgendamentoAdminUpdateDto> updateValidator,
            ILocalDescargaRepositorio descargaRepositorio,
            IFornecedorRepositorio fornecedorRepositorio,
            IUnidadeEntregaRepositorio entregaRepositorio,
            IAgendamentoRecebimentoLifecycleService lifecycle,
            IEmpresaRepositorio empresaRepo,
            ICurrentUserService currentUser,
            IProdutoRepositorio produtoRepositorio,
            ILocalDescargaRepositorio localDescargaRepositorio,
            ILogger<AgendamentoAdminService> logger
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _descargaRepo = descargaRepositorio;
            _fornecedorRepositorio = fornecedorRepositorio;
            _unidadeRepo = entregaRepositorio;
            _lifecycle = lifecycle;
            _empresaRepo = empresaRepo;
            _currentUser = currentUser;
            _produtoRepo = produtoRepositorio;
            _localRepo = localDescargaRepositorio;
            _logger = logger;
        }

        public async Task<AgendamentoAdminResponse> CreateAvulso(
            AgendamentoAdminCreateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _createValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao criar agendamento avulso: {Errors}",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var empresaId = _currentUser.EmpresaId ?? throw new UnauthorizedAccessException("Usuário sem empresa.");

            var empresa = await _empresaRepo.GetById(empresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            Fornecedor? fornecedor = null;
            if (dto.FornecedorId.HasValue)
            {
                fornecedor = await _fornecedorRepositorio.GetById(dto.FornecedorId.Value, token)
                    ?? throw new NotFoundException("Fornecedor não encontrado");
            }

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

            var inicioUtc = DateTime.SpecifyKind(dto.DataInicio, DateTimeKind.Utc);
            var fimUtc = DateTime.SpecifyKind(dto.DataFim, DateTimeKind.Utc);

            await EnsureNoConflitoAsync(
                localDescarga.Id,
                inicioUtc,
                fimUtc,
                excludeAgendamentoId: null,
                token);

            var statusInicial = dto.MotoristaId.HasValue || !string.IsNullOrWhiteSpace(dto.PlacaVeiculo)
                ? StatusAgendamento.Agendado
                : StatusAgendamento.Disponivel;

            var vaga = new Agendamento
            {
                FornecedorId = dto.FornecedorId,
                Produto = produto,
                ProdutoId = produto?.Id,
                TipoCarga = dto.TipoCarga,
                UnidadeEntregaId = localDescarga.UnidadeEntregaId,
                UnidadeEntrega = unidade,
                LocalDescargaId = localDescarga.Id,
                LocalDescarga = localDescarga,
                DataInicio = DateTime.SpecifyKind(dto.DataInicio, DateTimeKind.Utc),
                DataFim = DateTime.SpecifyKind(dto.DataFim, DateTimeKind.Utc),
                UsuarioId = dto.MotoristaId,
                NotaFiscalId = dto.NotaFiscalId,
                VolumeCarga = dto.VolumeCarga,
                PlacaVeiculo = dto.PlacaVeiculo,
                TipoVeiculo = dto.TipoVeiculo,
                EmpresaId = empresa.Id,
                Grade = null,
                GradeId = null,
                StatusAgendamento = statusInicial
            };

            await using var tx = await _repo.BeginTransactionAsync(token);
            try
            {
                await _repo.AddAgendamento(vaga, token);

                if (statusInicial == StatusAgendamento.Agendado && dto.VolumeCarga > 0)
                {
                    await _lifecycle.AoReservarAsync(vaga, dto.VolumeCarga, token);
                }

                await tx.CommitAsync(token);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23P01")
            {
                throw new BusinessException(
                    "Conflito detectado: outro agendamento foi criado em paralelo " +
                    "nesta doca para um horário sobreposto. Atualize a tela e tente novamente.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
            {
                throw new BusinessException(
                    "Este agendamento já possui uma reserva ativa de recebimento. " +
                    "Atualize a tela e tente novamente.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                throw new BusinessException(
                    "O planejamento de recebimento foi alterado em paralelo. " +
                    "Atualize a tela e tente novamente.");
            }

            _logger.LogInformation(
                "Agendamento avulso criado: {AgendamentoId} (empresa {EmpresaId}, status {Status})",
                vaga.Id, empresaId, vaga.StatusAgendamento);

            return MapToResponse(vaga);
        }
        public async Task<PagedResponse<AgendamentoAdminResponse>> GetByFiltros
            (
                AgendamentoFilterDto filtros,
                CancellationToken token = default
            )
        {
            var dataInicio = filtros.DataInicio.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(filtros.DataInicio.Value.Date, DateTimeKind.Unspecified),
                    Grade.OperationalTimeZone)
                : (DateTime?)null;

            var dataFim = filtros.DataFim.HasValue
                ? TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(
                        filtros.DataFim.Value.Date.AddDays(1).AddTicks(-1),
                        DateTimeKind.Unspecified),
                    Grade.OperationalTimeZone)
                : (DateTime?)null;

            if (dataInicio.HasValue && dataFim.HasValue && dataFim < dataInicio)
            {
                throw new BusinessException("A data final deve ser maior que a inicial.");
            }

            filtros.DataInicio = dataInicio;
            filtros.DataFim = dataFim;

            return await _repo.GetAdminViewAsync(filtros, token);
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

            _logger.LogInformation(
                "Chegada registrada no agendamento {AgendamentoId} (empresa {EmpresaId})",
                agendamentoId, agendamento.EmpresaId);
        }

        public async Task FinalizarOperacao(
            Guid agendamentoId,
            CancellationToken token = default)
        {
            var agendamento = await _repo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado");

            agendamento.FinalizarOperacao();
            await _repo.Update(agendamento, token);

            _logger.LogInformation(
                "Operação finalizada no agendamento {AgendamentoId} (empresa {EmpresaId})",
                agendamentoId, agendamento.EmpresaId);
        }

        public async Task CancelarAgendamento(
            Guid agendamentoId,
            CancellationToken token = default
            )
        {
            var agendamento = await _repo.GetById(agendamentoId, token)
                ?? throw new NotFoundException("Agendamento não encontrado.");

            await using var tx = await _repo.BeginTransactionAsync(token);
            try
            {
                await _lifecycle.AoCancelarOuExpirarAsync(agendamento, token);

                agendamento.Cancelar();
                await _repo.Update(agendamento, token);

                await tx.CommitAsync(token);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                throw new BusinessException(
                    "O agendamento ou o planejamento foi alterado em paralelo. " +
                    "Atualize a tela e tente novamente.");
            }

            _logger.LogInformation(
                "Agendamento cancelado: {AgendamentoId} (empresa {EmpresaId})",
                agendamentoId, agendamento.EmpresaId);
        }
        public async Task<AgendamentoAdminResponse> Update(
            Guid id,
            AgendamentoAdminUpdateDto dto,
            CancellationToken token = default
            )
        {
            var validation = await _updateValidator.ValidateAsync(dto, token);

            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Validação falhou ao atualizar agendamento {AgendamentoId}: {Errors}",
                    id, string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
                throw new ValidationException(validation.Errors);
            }

            var agendamento = await _repo.GetById(id, token)
                ?? throw new NotFoundException("Agendamento não encontrado");

            var novaDoca = await _unidadeRepo.GetById(dto.UnidadeEntregaId, token)
                ?? throw new BusinessException("Doca inválida");

            var pesoAnterior = agendamento.VolumeCarga ?? 0m;

            agendamento.UnidadeEntregaId = dto.UnidadeEntregaId;
            agendamento.DataInicio = DateTime.SpecifyKind(dto.DataInicio, DateTimeKind.Utc);
            agendamento.DataFim = DateTime.SpecifyKind(dto.DataFim, DateTimeKind.Utc);

            agendamento.UsuarioId = dto.MotoristaId;
            agendamento.NotaFiscalId = dto.NotaFiscalId;

            agendamento.UpdatedAt = DateTime.UtcNow;

            await EnsureNoConflitoAsync(
                agendamento.LocalDescargaId,
                agendamento.DataInicio,
                agendamento.DataFim,
                excludeAgendamentoId: agendamento.Id,
                token);

            await using var tx = await _repo.BeginTransactionAsync(token);
            try
            {
                await _repo.Update(agendamento, token);

                var pesoAtual = agendamento.VolumeCarga ?? 0m;
                if (agendamento.StatusAgendamento == StatusAgendamento.Agendado
                    || agendamento.StatusAgendamento == StatusAgendamento.EmAndamento)
                {
                    if (pesoAnterior != pesoAtual)
                    {
                        await _lifecycle.AoAtualizarReservaAsync(agendamento, pesoAtual, token);
                    }
                }

                await tx.CommitAsync(token);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23P01")
            {
                throw new BusinessException(
                    "Conflito detectado: outro agendamento ocupa esta doca em horário " +
                    "sobreposto à nova janela informada.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                throw new BusinessException(
                    "O agendamento ou o planejamento foi alterado em paralelo. " +
                    "Atualize a tela e tente novamente.");
            }

            _logger.LogInformation(
                "Agendamento atualizado: {AgendamentoId} (empresa {EmpresaId})",
                id, agendamento.EmpresaId);

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

            var agendamento = await _repo.GetById(agendamentoId, token)
                    ?? throw new NotFoundException("Agendamento não encontrado.");

            await using var tx = await _repo.BeginTransactionAsync(token);
            try
            {
                agendamento.FinalizarOperacao();

                await _lifecycle.AoFinalizarAsync(agendamento, quantidadeRecebida, token);

                await _repo.Update(agendamento, token);

                await tx.CommitAsync(token);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
                when (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
            {
                throw new BusinessException(
                    "Este agendamento já foi finalizado. Atualize a tela.");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                throw new BusinessException(
                    "O agendamento ou o planejamento foi alterado em paralelo. " +
                    "Atualize a tela e tente novamente.");
            }

            _logger.LogInformation(
                "Agendamento finalizado: {AgendamentoId} qtd {Quantidade} (empresa {EmpresaId})",
                agendamentoId, quantidadeRecebida, empresaId);
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

            if (agendamento.StatusAgendamento == StatusAgendamento.Finalizado)
            {
                throw new BusinessException("Não é possível remover um agendamento finalizado.");
            }

            await _repo.Delete(agendamento, token);

            _logger.LogInformation(
                "Agendamento excluído: {AgendamentoId} (empresa {EmpresaId})",
                id, agendamento.EmpresaId);
        }

        private async Task EnsureNoConflitoAsync(
            Guid? localDescargaId,
            DateTime inicioUtc,
            DateTime fimUtc,
            Guid? excludeAgendamentoId,
            CancellationToken token)
        {
            if (!localDescargaId.HasValue)
            {
                return;
            }

            var conflitos = await _repo.GetConflitosAsync(
                localDescargaId.Value,
                inicioUtc,
                fimUtc,
                excludeAgendamentoId,
                token);

            if (conflitos.Count == 0)
            {
                return;
            }

            var c = conflitos[0];
            var inicioBrt = TimeZoneInfo.ConvertTimeFromUtc(c.DataInicio, Grade.OperationalTimeZone);
            var fimBrt = TimeZoneInfo.ConvertTimeFromUtc(c.DataFim, Grade.OperationalTimeZone);
            var docaNome = c.LocalDescarga?.Nome ?? "esta doca";
            var produtoNome = c.Produto?.Nome ?? "carga geral";

            throw new BusinessException(
                $"Conflito: a doca '{docaNome}' já tem um agendamento de '{produtoNome}' " +
                $"entre {inicioBrt:dd/MM/yyyy HH:mm} e {fimBrt:dd/MM/yyyy HH:mm}.");
        }

        private static AgendamentoAdminResponse MapToResponse(Agendamento agendamento)
        {
            return new AgendamentoAdminResponse
            {
                Id = agendamento.Id,
                FornecedorNome = agendamento.Fornecedor?.Nome,
                MotoristaNome = agendamento.Usuario?.Motorista?.NomeReal,
                MotoristaTelefone = agendamento.Usuario?.Motorista?.Telefone,
                DataInicio = agendamento.DataInicio,
                DataFim = agendamento.DataFim,
                Produto = agendamento.Grade?.Produto?.Nome ?? agendamento?.Produto?.Nome!,
                PesoCarga = agendamento.VolumeCarga ?? agendamento.NotaFiscal?.PesoBruto ?? 0,
                PlacaVeiculo = agendamento.PlacaVeiculo ?? agendamento.NotaFiscal?.PlacaVeiculo,
                LocalDescarga = agendamento.LocalDescarga?.Nome,
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
