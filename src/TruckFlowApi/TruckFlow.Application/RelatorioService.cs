using System.Globalization;
using System.Runtime.CompilerServices;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RelatorioService : IRelatorioService
    {
        private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

        private readonly IAgendamentoRepositorio _repo;
        private readonly TimeProvider _timeProvider;

        public RelatorioService(IAgendamentoRepositorio repo, TimeProvider timeProvider)
        {
            _repo = repo;
            _timeProvider = timeProvider;
        }

        public async Task<RelatorioArquivoDto> GerarRelatorioAgendamentos(
            RelatorioAgendamentoFilterDto filtros,
            FormatoRelatorio formato,
            CancellationToken token = default)
        {
            if (!Enum.IsDefined(formato))
            {
                throw new BusinessException("Formato de relatório inválido.");
            }

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtros);

            var limite = formato switch
            {
                FormatoRelatorio.Csv => RelatorioLimites.MaxLinhasCsv,
                FormatoRelatorio.Xlsx => RelatorioLimites.MaxLinhasXlsx,
                _ => RelatorioLimites.MaxLinhasPdf
            };

            var total = await _repo.CountLinhasRelatorioAsync(criteria, token);
            if (total > limite)
            {
                throw new BusinessException(MensagemLimiteExcedido(formato, total, limite));
            }

            var agoraLocal = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow(), Grade.OperationalTimeZone).DateTime;
            var nomeBase = $"relatorio-agendamentos-{agoraLocal:yyyyMMdd-HHmm}";

            if (formato == FormatoRelatorio.Csv)
            {
                return new RelatorioArquivoDto
                {
                    ContentType = "text/csv; charset=utf-8",
                    NomeArquivo = $"{nomeBase}.csv",
                    EscreverConteudo = async (destino, ct) =>
                    {
                        var fins = RelatorioDetalhadoCalculo.FinsPorDoca(await _repo.ListarFinsDescargaAsync(
                            criteria.DataInicioUtc.AddDays(-1), criteria.DataFimUtc.AddDays(2), null, ct));

                        await RelatorioCsvWriter.EscreverAsync(
                            ComCiclo(_repo.StreamLinhasRelatorioAsync(criteria, limite), fins), destino, ct);
                    }
                };
            }

            var linhas = await _repo.GetLinhasRelatorioAsync(criteria, limite, token);
            await AplicarCicloAsync(linhas, token);

            if (formato == FormatoRelatorio.Xlsx)
            {
                using var planilha = new MemoryStream();
                await RelatorioXlsxWriter.EscreverAsync(linhas, planilha, token);
                var xlsx = planilha.ToArray();

                return new RelatorioArquivoDto
                {
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    NomeArquivo = $"{nomeBase}.xlsx",
                    EscreverConteudo = (destino, ct) => destino.WriteAsync(xlsx, ct).AsTask()
                };
            }

            var pdf = RelatorioPdfBuilder.Gerar(linhas, criteria, agoraLocal);

            return new RelatorioArquivoDto
            {
                ContentType = "application/pdf",
                NomeArquivo = $"{nomeBase}.pdf",
                EscreverConteudo = (destino, ct) => destino.WriteAsync(pdf, ct).AsTask()
            };
        }

        public async Task<PagedResponse<AgendamentoDetalhadoDto>> ListarDetalhadoAsync(
            RelatorioAgendamentoDetalhadoQueryDto query,
            CancellationToken token = default)
        {
            var criteria = RelatorioFiltroNormalizer.Normalizar(query);
            var pageNumber = Math.Max(1, query.PageNumber);
            var pageSize = Math.Clamp(query.PageSize, 1, RelatorioLimites.MaxPorPagina);

            var pagina = await _repo.GetPaginaRelatorioAsync(criteria, pageNumber, pageSize, token);
            await AplicarCicloAsync(pagina.Items, token);

            return new PagedResponse<AgendamentoDetalhadoDto>
            {
                Items = pagina.Items.Select(RelatorioDetalhadoCalculo.Mapear).ToList(),
                PageNumber = pagina.PageNumber,
                PageSize = pagina.PageSize,
                TotalCount = pagina.TotalCount,
                TotalPages = pagina.TotalPages
            };
        }

        public async Task<RelatorioDetalhadoResumoDto> ResumoDetalhadoAsync(
            RelatorioAgendamentoFilterDto filtros,
            CancellationToken token = default)
        {
            var criteria = RelatorioFiltroNormalizer.Normalizar(filtros);
            var linhas = await _repo.ListarResumoRelatorioAsync(criteria, token);

            var fins = await FinsParaEstimarAsync(
                linhas.Select(l => (l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm)), token);

            return RelatorioDetalhadoCalculo.Resumo(linhas, fins);
        }

        private async Task AplicarCicloAsync(IReadOnlyCollection<AgendamentoRelatorioLinha> linhas, CancellationToken token)
        {
            var fins = await FinsParaEstimarAsync(
                linhas.Select(l => (l.LocalDescargaId, l.ChegouEm, l.InicioDescargaEm, l.FimDescargaEm)), token);

            foreach (var linha in linhas)
            {
                linha.Ciclo = RelatorioDetalhadoCalculo.Ciclo(linha, fins);
            }
        }

        private async Task<IReadOnlyDictionary<Guid, List<DateTime>>> FinsParaEstimarAsync(
            IEnumerable<(Guid? Doca, DateTime? ChegouEm, DateTime? InicioDescargaEm, DateTime? FimDescargaEm)> ciclos,
            CancellationToken token)
        {
            var estimaveis = ciclos
                .Where(c => c.InicioDescargaEm is null && c.ChegouEm.HasValue && c.FimDescargaEm.HasValue && c.Doca.HasValue)
                .ToList();

            if (estimaveis.Count == 0)
            {
                return RelatorioDetalhadoCalculo.FinsPorDoca([]);
            }

            var fins = await _repo.ListarFinsDescargaAsync(
                estimaveis.Min(c => c.ChegouEm!.Value),
                estimaveis.Max(c => c.FimDescargaEm!.Value),
                estimaveis.Select(c => c.Doca!.Value).Distinct().ToList(),
                token);

            return RelatorioDetalhadoCalculo.FinsPorDoca(fins);
        }

        private static async IAsyncEnumerable<AgendamentoRelatorioLinha> ComCiclo(
            IAsyncEnumerable<AgendamentoRelatorioLinha> linhas,
            IReadOnlyDictionary<Guid, List<DateTime>> fins,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await foreach (var linha in linhas.WithCancellation(token))
            {
                linha.Ciclo = RelatorioDetalhadoCalculo.Ciclo(linha, fins);
                yield return linha;
            }
        }

        private static string MensagemLimiteExcedido(FormatoRelatorio formato, int total, int limite) =>
            formato == FormatoRelatorio.Pdf
                ? $"A consulta retornou {total.ToString("N0", PtBr)} registros e o PDF suporta até {limite.ToString("N0", PtBr)}. Refine os filtros ou exporte em CSV."
                : $"A consulta retornou {total.ToString("N0", PtBr)} registros e o limite de exportação é {limite.ToString("N0", PtBr)}. Refine os filtros.";
    }
}
