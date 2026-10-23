using System.Globalization;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
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

            var limite = formato == FormatoRelatorio.Csv
                ? RelatorioLimites.MaxLinhasCsv
                : RelatorioLimites.MaxLinhasPdf;

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
                    EscreverConteudo = (destino, ct) => RelatorioCsvWriter.EscreverAsync(
                        _repo.StreamLinhasRelatorioAsync(criteria, limite), destino, ct)
                };
            }

            var linhas = await _repo.GetLinhasRelatorioAsync(criteria, limite, token);
            var pdf = RelatorioPdfBuilder.Gerar(linhas, criteria, agoraLocal);

            return new RelatorioArquivoDto
            {
                ContentType = "application/pdf",
                NomeArquivo = $"{nomeBase}.pdf",
                EscreverConteudo = (destino, ct) => destino.WriteAsync(pdf, ct).AsTask()
            };
        }

        private static string MensagemLimiteExcedido(FormatoRelatorio formato, int total, int limite) =>
            formato == FormatoRelatorio.Pdf
                ? $"A consulta retornou {total.ToString("N0", PtBr)} registros e o PDF suporta até {limite.ToString("N0", PtBr)}. Refine os filtros ou exporte em CSV."
                : $"A consulta retornou {total.ToString("N0", PtBr)} registros e o limite de exportação é {limite.ToString("N0", PtBr)}. Refine os filtros.";
    }
}
