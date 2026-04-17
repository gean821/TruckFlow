using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Factories
{
    public class RecebimentoFactory
    {
        private readonly IFornecedorRepositorio _fornecedorRepo;
        private readonly IProdutoRepositorio _produtoRepo;
        private readonly IEmpresaRepositorio _empresaRepo;

        public RecebimentoFactory(
            IFornecedorRepositorio fornecedorRepo,
            IProdutoRepositorio produtoRepo,
            IEmpresaRepositorio empresaRepo
            )
        {
            _fornecedorRepo = fornecedorRepo;
            _produtoRepo = produtoRepo;
            _empresaRepo = empresaRepo;
        }

        public async Task<PlanejamentoRecebimento> CreateRecebimentoFromDto(
            RecebimentoCreateDto dto,
            Guid empresaId,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var fornecedor = await _fornecedorRepo.GetById(dto.FornecedorId, token)
                ?? throw new NotFoundException("Fornecedor não encontrado.");

            var produtoIds = dto.ItensPlanejamento!.Select(x => x.ProdutoId).ToList();
            var produtos = await _produtoRepo.GetByIdsAsync(produtoIds, token);

            if (produtos.Count != produtoIds.Distinct().Count())
                throw new NotFoundException("Um ou mais produtos informados não foram encontrados.");

            var produtoDicionario = produtos.ToDictionary(p => p.Id);

            var empresa = await _empresaRepo.GetById(empresaId, token)
                ?? throw new NotFoundException("Empresa não encontrada.");

            var recebimento = new PlanejamentoRecebimento
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                DataInicio = DateTime.SpecifyKind(dto.DataInicio.Date, DateTimeKind.Utc),
                DataFim = DateTime.SpecifyKind(dto.DataFim.Date, DateTimeKind.Utc),
                EmpresaId = empresa.Id,
                Fornecedor = fornecedor,
                FornecedorId = fornecedor.Id,
                StatusRecebimento = StatusRecebimento.Planejado
            };

            recebimento.ItemPlanejamentos = dto.ItensPlanejamento!.Select(itemDto =>
            {
                var diasOperacao = ContarDiasOperacao(
                    dto.DataInicio,
                    dto.DataFim,
                    itemDto.DiasSemana);

                if (diasOperacao == 0)
                {
                    throw new BusinessException(
                        "Os dias da semana selecionados não ocorrem dentro da vigência do planejamento.");
                }

                return new ItemPlanejamento
                {
                    Id = Guid.NewGuid(),
                    PlanejamentoRecebimento = recebimento,
                    PlanejamentoRecebimentoId = recebimento.Id,
                    ProdutoId = itemDto.ProdutoId,
                    Produto = produtoDicionario[itemDto.ProdutoId],
                    CadenciaDiariaPlanejada = itemDto.CadenciaDiariaPlanejada,
                    QuantidadeTotalPlanejada = itemDto.CadenciaDiariaPlanejada * diasOperacao,
                    QuantidadeTotalRecebida = 0,
                    DiasSemana = NormalizarDias(itemDto.DiasSemana),
                    ToleranciaExtra = itemDto.ToleranciaExtra ?? 30m,
                    CreatedAt = DateTime.UtcNow,
                    EmpresaId = empresa.Id
                };
            }).ToList();

            return recebimento;
        }

        public static int ContarDiasOperacao(
            DateTime inicio,
            DateTime fim,
            string diasSemana)
        {
            var dias = ParseDias(diasSemana);
            if (dias.Count == 0) return 0;

            var total = 0;
            for (var cursor = inicio.Date; cursor <= fim.Date; cursor = cursor.AddDays(1))
            {
                if (dias.Contains(cursor.DayOfWeek)) total++;
            }
            return total;
        }

        public static HashSet<DayOfWeek> ParseDias(string diasSemana)
        {
            if (string.IsNullOrWhiteSpace(diasSemana))
                return new HashSet<DayOfWeek>();

            return diasSemana
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s))
                .Where(n => n is >= 0 and <= 6)
                .Select(n => (DayOfWeek)n)
                .ToHashSet();
        }

        public static string NormalizarDias(string diasSemana)
        {
            return string.Join(",",
                ParseDias(diasSemana)
                    .Select(d => (int)d)
                    .OrderBy(n => n));
        }
    }
}