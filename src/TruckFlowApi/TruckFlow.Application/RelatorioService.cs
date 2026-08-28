using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Enums;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class RelatorioService : IRelatorioService
    {
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
            var linhas = await _repo.GetForRelatorioAsync(filtros, token);
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
                    Conteudo = GerarPdf(linhas),
                    ContentType = "application/pdf",
                    NomeArquivo = $"relatorio-agendamentos-{timestamp}.pdf"
                },
                _ => throw new ArgumentOutOfRangeException(nameof(formato))
            };
        }

        // ============= Csv (Excel) =============
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
                    a.DataInicio.ToString("dd/MM/yyyy HH:mm"),
                    a.DataFim.ToString("dd/MM/yyyy HH:mm")));
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

        // ============= PDF =============
        private static byte[] GerarPdf(List<AgendamentoAdminResponse> linhas)
        {
            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text("Relatório de Agendamentos").FontSize(18).Bold();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Text("Fornecedor").Bold();
                            h.Cell().Text("Produto").Bold();
                            h.Cell().Text("Placa").Bold();
                            h.Cell().Text("Motorista").Bold();
                            h.Cell().Text("Peso").Bold();
                            h.Cell().Text("Data Início").Bold();
                        });

                        foreach (var a in linhas)
                        {
                            table.Cell().Text(a.FornecedorNome ?? "-");
                            table.Cell().Text(a.Produto);
                            table.Cell().Text(a.PlacaVeiculo ?? "-");
                            table.Cell().Text(a.MotoristaNome ?? "-");
                            table.Cell().Text(a.PesoCarga?.ToString("0.##") ?? "-");
                            table.Cell().Text(a.DataInicio.ToString("dd/MM/yyyy HH:mm"));
                        }
                    });

                    page.Footer().AlignCenter().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf(stream);

            return stream.ToArray();
        }
    }
}
