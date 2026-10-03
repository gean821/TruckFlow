using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RelatorioService : IRelatorioService
    {
        private const string CorMarca = "#195FA0";

        private readonly IAgendamentoRepositorio _repo;

        public RelatorioService(IAgendamentoRepositorio repo)
        {
            _repo = repo;
        }

        public async Task<RelatorioArquivoDto> GerarRelatorioAgendamentos(
            RelatorioAgendamentoFilterDto filtros,
            FormatoRelatorio formato,
            CancellationToken token = default)
        {
            if (!filtros.DataInicio.HasValue || !filtros.DataFim.HasValue)
            {
                throw new BusinessException("Informe o período (data início e data fim) para gerar o relatório.");
            }

            if (filtros.DataInicio.HasValue)
            {
                filtros.DataInicio = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(filtros.DataInicio.Value.Date, DateTimeKind.Unspecified),
                    Grade.OperationalTimeZone);
            }

            if (filtros.DataFim.HasValue)
            {
                filtros.DataFim = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(filtros.DataFim.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified),
                    Grade.OperationalTimeZone);
            }

            var linhas = await _repo.GetForRelatorioAsync(filtros, token);

            if (filtros.HoraInicio.HasValue || filtros.HoraFim.HasValue)
            {
                linhas = linhas
                    .Where(a =>
                    {
                        var horaLocal = TimeZoneInfo
                            .ConvertTimeFromUtc(a.DataInicio, Grade.OperationalTimeZone)
                            .TimeOfDay;

                        var depoisDoInicio = !filtros.HoraInicio.HasValue || horaLocal >= filtros.HoraInicio.Value;
                        var antesDoFim = !filtros.HoraFim.HasValue || horaLocal <= filtros.HoraFim.Value;

                        return depoisDoInicio && antesDoFim;
                    })
                    .ToList();
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm");

            return formato switch
            {
                FormatoRelatorio.Csv => new RelatorioArquivoDto
                {
                    Conteudo = GerarCsv(linhas),
                    ContentType = "text/csv",
                    NomeArquivo = $"relatorio-agendamentos-{timestamp}.csv"
                },
                FormatoRelatorio.Pdf => new RelatorioArquivoDto
                {
                    Conteudo = GerarPdf(linhas, filtros),
                    ContentType = "application/pdf",
                    NomeArquivo = $"relatorio-agendamentos-{timestamp}.pdf"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(formato))
            };
        }

        private static string FormatarDataHoraLocal(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(utc, Grade.OperationalTimeZone).ToString("dd/MM/yyyy HH:mm");

        // ============= Csv =============
        private static byte[] GerarCsv(List<AgendamentoAdminResponse> linhas)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fornecedor;Produto;Placa;Motorista;Unidade;Local;TipoVeiculo;Peso;Status;DataInicio;DataFim");

            foreach (var a in linhas)
            {
                sb.AppendLine(string.Join(';',
                    Csv(a.FornecedorNome),
                    Csv(a.Produto),
                    Csv(a.PlacaVeiculo),
                    Csv(a.MotoristaNome),
                    Csv(a.UnidadeEntrega),
                    Csv(a.LocalDescarga),
                    Csv(a.TipoVeiculo),
                    a.PesoCarga?.ToString("0.##") ?? "",
                    Csv(a.Status),
                    FormatarDataHoraLocal(a.DataInicio),
                    FormatarDataHoraLocal(a.DataFim)));
            }

            var bom = new byte[] { 0xEF, 0xBB, 0xBF };
            var corpo = Encoding.UTF8.GetBytes(sb.ToString());
            return bom.Concat(corpo).ToArray();
        }

        private static string Csv(string? valor)
        {
            if (string.IsNullOrEmpty(valor)) return "";
            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n'))
                return $"\"{valor.Replace("\"", "\"\"")}\"";
            return valor;
        }

        // ============= Pdf =============
        private static byte[] GerarPdf(List<AgendamentoAdminResponse> linhas, RelatorioAgendamentoFilterDto filtros)
        {
            using var stream = new MemoryStream();
            var logoPng = CarregarLogoPng();

            Document.Create(container =>
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
                            if (logoPng.Length > 0)
                            {
                                row.ConstantItem(140).Image(logoPng).FitWidth();
                            }

                            row.RelativeItem().Column(titulo =>
                            {
                                titulo.Item().AlignRight().Text("Relatório de Agendamentos")
                                    .FontSize(16).Bold().FontColor(CorMarca);

                                titulo.Item().AlignRight().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}")
                                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(CorMarca);
                    });

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Element(c => FiltrosAplicadosSection(c, filtros));

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
            }).GeneratePdf(stream);

            return stream.ToArray();
        }

        private static void FiltrosAplicadosSection(IContainer container, RelatorioAgendamentoFilterDto filtros)
        {
            var itens = new List<string>();

            if (filtros.DataInicio.HasValue)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(filtros.DataInicio.Value, Grade.OperationalTimeZone);
                itens.Add($"De: {local:dd/MM/yyyy}");
            }

            if (filtros.DataFim.HasValue)
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(filtros.DataFim.Value, Grade.OperationalTimeZone);
                itens.Add($"Até: {local:dd/MM/yyyy}");
            }

            if (filtros.HoraInicio.HasValue) itens.Add($"A partir das: {filtros.HoraInicio.Value:hh\\:mm}");
            if (filtros.HoraFim.HasValue) itens.Add($"Até as: {filtros.HoraFim.Value:hh\\:mm}");
            if (!string.IsNullOrWhiteSpace(filtros.PlacaVeiculo)) itens.Add($"Placa: {filtros.PlacaVeiculo}");
            if (!string.IsNullOrWhiteSpace(filtros.Motorista)) itens.Add($"Motorista: {filtros.Motorista}");

            if (itens.Count == 0)
            {
                return;
            }

            container.Background(Colors.Grey.Lighten4).Padding(8).Column(column =>
            {
                column.Item().Text("Filtros aplicados").FontSize(9).Bold().FontColor(CorMarca);
                column.Item().PaddingTop(4).Text(string.Join("   •   ", itens)).FontSize(8.5f);
            });
        }

        private static void TabelaAgendamentos(IContainer container, List<AgendamentoAdminResponse> linhas)
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
                    Celula(a.PesoCarga?.ToString("N0", CultureInfo.GetCultureInfo("pt-BR")) ?? "-");
                    Celula(a.Status);
                    Celula(FormatarDataHoraLocal(a.DataInicio));
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
