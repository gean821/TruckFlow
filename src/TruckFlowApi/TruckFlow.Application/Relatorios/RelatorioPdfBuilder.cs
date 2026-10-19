using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckFlow.Domain.Dto.Relatorio;

namespace TruckFlow.Application.Relatorios
{
    public static class RelatorioPdfBuilder
    {
        private const string CorMarca = "#195FA0";

        // Lido do assembly uma única vez (antes era a cada request). Lazy é thread-safe por padrão.
        private static readonly Lazy<byte[]> LogoPng = new(CarregarLogoPng);

        public static byte[] Gerar(
            IReadOnlyList<AgendamentoRelatorioLinha> linhas,
            RelatorioAgendamentoCriteria criteria,
            DateTime geradoEmLocal)
        {
            var logo = LogoPng.Value;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().Row(row =>
                        {
                            if (logo.Length > 0)
                            {
                                row.ConstantItem(140).Image(logo).FitWidth();
                            }

                            row.RelativeItem().Column(titulo =>
                            {
                                titulo.Item().AlignRight().Text("Relatório de Agendamentos")
                                    .FontSize(16).Bold().FontColor(CorMarca);

                                titulo.Item().AlignRight().Text($"Gerado em {geradoEmLocal:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(CorMarca);
                    });

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Element(c => FiltrosAplicadosSection(c, criteria));

                        column.Item().Text($"{linhas.Count} agendamento(s) encontrado(s)")
                            .FontSize(9).Bold().FontColor(CorMarca);

                        column.Item().Element(c => TabelaAgendamentos(c, linhas));
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem().Text("TruckFlow").FontSize(8).FontColor(Colors.Grey.Darken1);

                        row.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Página ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                            x.Span(" de ").FontSize(8).FontColor(Colors.Grey.Darken1);
                            x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    });
                });
            }).GeneratePdf();
        }

        private static void FiltrosAplicadosSection(IContainer container, RelatorioAgendamentoCriteria criteria)
        {
            var itens = new List<string>
            {
                $"De: {RelatorioFormatacao.ParaHorarioLocal(criteria.DataInicioUtc):dd/MM/yyyy}",
                $"Até: {RelatorioFormatacao.ParaHorarioLocal(criteria.DataFimUtc):dd/MM/yyyy}"
            };

            if (criteria.HoraInicio.HasValue) itens.Add($"A partir das: {criteria.HoraInicio.Value:hh\\:mm}");
            if (criteria.HoraFim.HasValue) itens.Add($"Até as: {criteria.HoraFim.Value:hh\\:mm}");
            if (criteria.PlacaVeiculo is not null) itens.Add($"Placa: {criteria.PlacaVeiculo}");
            if (criteria.Motorista is not null) itens.Add($"Motorista: {criteria.Motorista}");

            container.Background(Colors.Grey.Lighten4).Padding(8).Column(column =>
            {
                column.Item().Text("Filtros aplicados").FontSize(9).Bold().FontColor(CorMarca);
                column.Item().PaddingTop(4).Text(string.Join("   •   ", itens)).FontSize(8.5f);
            });
        }

        private static void TabelaAgendamentos(IContainer container, IReadOnlyList<AgendamentoRelatorioLinha> linhas)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.2f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.1f);
                    c.RelativeColumn(2f);
                    c.RelativeColumn(1.6f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.4f);
                    c.RelativeColumn(1.8f);
                });

                table.Header(header =>
                {
                    void CelulaHeader(string texto) =>
                        header.Cell().Background(CorMarca).Padding(6)
                            .Text(texto).FontColor(Colors.White).Bold().FontSize(8.5f);

                    CelulaHeader("Fornecedor");
                    CelulaHeader("Produto");
                    CelulaHeader("Placa");
                    CelulaHeader("Motorista");
                    CelulaHeader("Unidade");
                    CelulaHeader("Peso (kg)");
                    CelulaHeader("Status");
                    CelulaHeader("Data Início");
                });

                for (var i = 0; i < linhas.Count; i++)
                {
                    var a = linhas[i];
                    var fundo = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                    void Celula(string texto) =>
                        table.Cell().Background(fundo).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .Padding(6).Text(texto).FontSize(8.5f);

                    Celula(a.FornecedorNome ?? "-");
                    Celula(a.Produto);
                    Celula(a.PlacaVeiculo ?? "-");
                    Celula(a.MotoristaNome ?? "-");
                    Celula(a.UnidadeEntrega ?? "-");
                    Celula(a.PesoCarga?.ToString("N0", RelatorioFormatacao.PtBr) ?? "-");
                    Celula(a.Status.ToString());
                    Celula(RelatorioFormatacao.DataHoraLocal(a.DataInicio));
                }
            });
        }

        private static byte[] CarregarLogoPng()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));

            if (resourceName is null)
            {
                return Array.Empty<byte>();
            }

            using var resourceStream = assembly.GetManifestResourceStream(resourceName)!;
            using var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
