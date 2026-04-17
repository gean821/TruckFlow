using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class PlanejamentoRecebimentoService : IPlanejamentoRecebimentoService
    {
        private readonly IPlanejamentoRecebimentoRepositorio _planeRepo;
        private readonly IFornecedorRepositorio _forRepo;
        private readonly IValidator<RecebimentoCreateDto> _createValidator;
        private readonly IValidator<RecebimentoUpdateDto> _updateValidator;
        private readonly RecebimentoFactory _factory;
        private readonly IProdutoRepositorio _produtoRepositorio;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly CurrentUserGuard _currentUser;
        public PlanejamentoRecebimentoService
            (
                IPlanejamentoRecebimentoRepositorio planeRepo,
                IValidator<RecebimentoCreateDto> createValidator,
                IValidator<RecebimentoUpdateDto> updateValidator,
                RecebimentoFactory factory,
                IFornecedorRepositorio repo,
                IProdutoRepositorio produtoRepositorio,
                IEmpresaRepositorio empresaRepo,
                CurrentUserGuard guard
            )
        {
            _planeRepo = planeRepo;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _factory = factory;
            _forRepo = repo;
            _produtoRepositorio = produtoRepositorio;
            _empresaRepo = empresaRepo;
            _currentUser = guard;
        }

        public async Task<RecebimentoResponseDto> CreateRecebimento
            (
                RecebimentoCreateDto recebimento,
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _createValidator.ValidateAsync(recebimento, token);
            
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var empresaId = _currentUser.GetEmpresaId();

            var novoRecebimento = await _factory.CreateRecebimentoFromDto(recebimento, empresaId, token);

            var recebimentoCriado = await _planeRepo.CreateRecebimento(novoRecebimento, token);
            
            await _planeRepo.SaveChangesAsync(token);

            return MapToResponse(recebimentoCriado);
        }
      
        public async Task<RecebimentoResponseDto> GetById(Guid id, CancellationToken token = default)
        {
            var recebimento = await _planeRepo.GetById(id, token) 
                ?? throw new NotFoundException("Recebimento não encontrado");

            return MapToResponse(recebimento);
        }

        public async Task DeleteRecebimento(Guid id, CancellationToken token = default)
        {
            var recebimento = await _planeRepo.GetById(id, token) ?? 
                throw new NotFoundException("Recebimento não encontrado");

            await _planeRepo.DeleteRecebimento(recebimento.Id, token);
            await _planeRepo.SaveChangesAsync(token);
        }

        public async Task<RecebimentoResponseDto> UpdateRecebimento
            (
                Guid id,
                RecebimentoUpdateDto recebimento,
                CancellationToken token = default
            )
        {
            ValidationResult validationResult = await _updateValidator.ValidateAsync(recebimento, token);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var recebimentoEncontrado = await _planeRepo.GetById(id, token)
                ?? throw new NotFoundException("recebimento não encontrado.");

            var fornecedor = await _forRepo.GetById(recebimento.FornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado.");

            var produtosIds = recebimento.ItensPlanejamento!.Select(x => x.ProdutoId).ToList();
            var produtos = await _produtoRepositorio.GetByIdsAsync(produtosIds, token);

            var empresaId = _currentUser.GetEmpresaId();
            var empresa = await _empresaRepo.GetById(empresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");


            recebimentoEncontrado.Fornecedor = fornecedor;
            recebimentoEncontrado.FornecedorId = fornecedor.Id;
            recebimentoEncontrado.DataInicio = DateTime.SpecifyKind(recebimento.DataInicio.Date, DateTimeKind.Utc);
            recebimentoEncontrado.DataFim = DateTime.SpecifyKind(recebimento.DataFim.Date, DateTimeKind.Utc);
            recebimentoEncontrado.StatusRecebimento = StatusRecebimento.Planejado;
            recebimentoEncontrado.UpdatedAt = DateTime.UtcNow;

            recebimentoEncontrado.ItemPlanejamentos = recebimento.ItensPlanejamento!
                .Select(x =>
                {
                    var produto = produtos.FirstOrDefault(p => p.Id == x.ProdutoId)
                        ?? throw new NotFoundException($"Produto com ID {x.ProdutoId} não encontrado.");

                    var diasOperacao = RecebimentoFactory.ContarDiasOperacao(
                        recebimento.DataInicio,
                        recebimento.DataFim,
                        x.DiasSemana);

                    if (diasOperacao == 0)
                    {
                        throw new BusinessException(
                            "Os dias da semana selecionados não ocorrem dentro da vigência do planejamento.");
                    }

                    return new ItemPlanejamento
                    {
                        ProdutoId = produto.Id,
                        Produto = produto,
                        Empresa = empresa,
                        PlanejamentoRecebimentoId = recebimentoEncontrado.Id,
                        PlanejamentoRecebimento = recebimentoEncontrado,
                        CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada,
                        QuantidadeTotalPlanejada = x.CadenciaDiariaPlanejada * diasOperacao,
                        DiasSemana = RecebimentoFactory.NormalizarDias(x.DiasSemana),
                        ToleranciaExtra = x.ToleranciaExtra ?? 30m
                    };
                })
                .ToList();

            var recebimentoAtualizado = await _planeRepo.UpdateRecebimento(id, recebimentoEncontrado, token);
            return MapToResponse(recebimentoAtualizado);
        }

        public async Task<List<RecebimentoResponseDto>> GetAll(CancellationToken token = default)
        {
            var listaRecebimento = await _planeRepo.GetAll(token);
            return listaRecebimento.Select(MapToResponse).ToList();
        }

        public async Task<PagedResponse<RecebimentoResponseDto>> GetPaged(
            PlanejamentoListQueryDto query,
            CancellationToken token = default)
        {
            var result = await _planeRepo.GetPagedAsync(query, token);

            return new PagedResponse<RecebimentoResponseDto>
            {
                Items = result.Items.Select(MapToResponse).ToList(),
                PageNumber = result.PageNumber,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages
            };
        }

        public async Task<PlanejamentoDashboardDto> GetDashboard(
            Guid id,
            DateTime? dataReferencia,
            CancellationToken token = default)
        {
            var planejamento = await _planeRepo.GetByIdWithEventos(id, token)
                ?? throw new NotFoundException("Planejamento não encontrado.");

            var dia = (dataReferencia ?? DateTime.UtcNow).Date;

            var itensDto = planejamento.ItemPlanejamentos.Select(item =>
            {
                var operaNoDia = item.OperaEm(dia);
                var recebidoNoDia = item.QuantidadeRecebidaNoDia(dia);
                var limiteDia = item.CadenciaDiariaPlanejada + item.ToleranciaExtra;
                var faltaNoDia = operaNoDia
                    ? Math.Max(0m, item.CadenciaDiariaPlanejada - recebidoNoDia)
                    : 0m;

                return new PlanejamentoDashboardItemDto
                {
                    Id = item.Id,
                    ProdutoId = item.ProdutoId,
                    Produto = item.Produto?.Nome ?? string.Empty,
                    CadenciaDiariaPlanejada = item.CadenciaDiariaPlanejada,
                    ToleranciaExtra = item.ToleranciaExtra,
                    QuantidadeTotalPlanejada = item.QuantidadeTotalPlanejada,
                    QuantidadeTotalRecebida = item.QuantidadeTotalRecebida,
                    FaltaReceber = Math.Max(0m, item.QuantidadeTotalPlanejada - item.QuantidadeTotalRecebida),
                    RecebidoNoDia = recebidoNoDia,
                    FaltaNoDia = faltaNoDia,
                    OperaNoDia = operaNoDia,
                    Congelado = operaNoDia && recebidoNoDia >= limiteDia,
                    DiasSemana = item.DiasSemana
                };
            }).ToList();

            return new PlanejamentoDashboardDto
            {
                Id = planejamento.Id,
                FornecedorId = planejamento.FornecedorId,
                FornecedorNome = planejamento.Fornecedor?.Nome ?? string.Empty,
                DataInicio = planejamento.DataInicio,
                DataFim = planejamento.DataFim,
                Status = planejamento.StatusRecebimento.ToString(),
                DataReferencia = dia,
                TotalDiario = itensDto.Sum(i => i.OperaNoDia ? i.CadenciaDiariaPlanejada : 0m),
                TotalDiarioRecebido = itensDto.Sum(i => i.RecebidoNoDia),
                TotalDiarioRestante = itensDto.Sum(i => i.FaltaNoDia),
                TotalPlanejado = itensDto.Sum(i => i.QuantidadeTotalPlanejada),
                TotalRecebido = itensDto.Sum(i => i.QuantidadeTotalRecebida),
                TotalRestante = itensDto.Sum(i => i.FaltaReceber),
                Itens = itensDto
            };
        }

        public async Task<PlanejamentoRelatorioDto> GetRelatorio(Guid id, CancellationToken token = default)
        {
            var planejamento = await _planeRepo.GetByIdWithEventos(id, token)
                ?? throw new NotFoundException("Planejamento não encontrado.");

            var itens = planejamento.ItemPlanejamentos.Select(item =>
            {
                var percent = item.QuantidadeTotalPlanejada > 0
                    ? Math.Min(100m, item.QuantidadeTotalRecebida / item.QuantidadeTotalPlanejada * 100m)
                    : 0m;

                return new PlanejamentoRelatorioItemDto
                {
                    Id = item.Id,
                    Produto = item.Produto?.Nome ?? string.Empty,
                    QuantidadeTotalPlanejada = item.QuantidadeTotalPlanejada,
                    QuantidadeTotalRecebida = item.QuantidadeTotalRecebida,
                    FaltaReceber = Math.Max(0m, item.QuantidadeTotalPlanejada - item.QuantidadeTotalRecebida),
                    PercentualAtingido = Math.Round(percent, 2),
                    DiasSemana = item.DiasSemana
                };
            }).ToList();

            var eventos = planejamento.ItemPlanejamentos
                .SelectMany(i => (i.RecebimentoEventos ?? Enumerable.Empty<RecebimentoEvento>())
                    .Select(e => new PlanejamentoRelatorioEventoDto
                    {
                        Id = e.Id,
                        AgendamentoId = e.AgendamentoId,
                        Produto = i.Produto?.Nome ?? string.Empty,
                        DataRecebimento = e.DataRecebimento,
                        Quantidade = e.Quantidade,
                        Observacao = e.Observacao
                    }))
                .OrderBy(e => e.DataRecebimento)
                .ToList();

            var totalPlanejado = itens.Sum(i => i.QuantidadeTotalPlanejada);
            var totalRecebido = itens.Sum(i => i.QuantidadeTotalRecebida);
            var percentualGeral = totalPlanejado > 0
                ? Math.Round(Math.Min(100m, totalRecebido / totalPlanejado * 100m), 2)
                : 0m;

            return new PlanejamentoRelatorioDto
            {
                Id = planejamento.Id,
                FornecedorNome = planejamento.Fornecedor?.Nome ?? string.Empty,
                DataInicio = planejamento.DataInicio,
                DataFim = planejamento.DataFim,
                Status = planejamento.StatusRecebimento.ToString(),
                TotalPlanejado = totalPlanejado,
                TotalRecebido = totalRecebido,
                TotalRestante = Math.Max(0m, totalPlanejado - totalRecebido),
                PercentualAtingido = percentualGeral,
                Itens = itens,
                Eventos = eventos
            };
        }

        public async Task Encerrar(Guid id, CancellationToken token = default)
        {
            var planejamento = await _planeRepo.GetById(id, token)
                ?? throw new NotFoundException("Planejamento não encontrado.");

            planejamento.StatusRecebimento = StatusRecebimento.Encerrado;
            planejamento.UpdatedAt = DateTime.UtcNow;

            await _planeRepo.UpdateRecebimento(id, planejamento, token);
            await _planeRepo.SaveChangesAsync(token);
        }

        private static RecebimentoResponseDto MapToResponse(PlanejamentoRecebimento recebimento) =>
            new RecebimentoResponseDto
            {
                Id = recebimento.Id,
                FornecedorId = recebimento.FornecedorId,
                FornecedorNome = recebimento.Fornecedor?.Nome ?? string.Empty,
                DataInicio = recebimento.DataInicio,
                DataFim = recebimento.DataFim,
                CreatedAt = recebimento.CreatedAt,
                TotalItens = recebimento.ItemPlanejamentos.Count,
                Status = recebimento.StatusRecebimento.ToString(),
                Itens = recebimento.ItemPlanejamentos!.Select(x => new ItemPlanejamentoResponseDto
                {
                    Id = x.Id,
                    CadenciaDiariaPlanejada = x.CadenciaDiariaPlanejada,
                    Produto = x.Produto!.Nome,
                    QuantidadeTotalPlanejada = x.QuantidadeTotalPlanejada,
                    QuantidadeTotalRecebida = x.QuantidadeTotalRecebida,
                    FaltaReceber = Math.Max(0m, x.QuantidadeTotalPlanejada - x.QuantidadeTotalRecebida),
                    DiasSemana = x.DiasSemana,
                    ToleranciaExtra = x.ToleranciaExtra,
                    Fornecedor = recebimento.Fornecedor?.Nome ?? string.Empty,
                    CreatedAt = x.CreatedAt
                }).ToList()
            };
    }
}
