using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Relatorio;

namespace TruckFlow.Application.Relatorios
{
    public static class RelatorioCsvWriter
    {
        public const string Cabecalho =
            "Fornecedor;Produto;Placa;Motorista;Unidade;Local;TipoVeiculo;Peso;Status;DataInicio;DataFim;" +
            "Origem;NotaFiscal;Chegada;SituacaoChegada;InicioDescarga;InicioEstimado;FimDescarga;Saida;EsperaMin;DescargaMin;PermanenciaMin";

        private static readonly UTF8Encoding Utf8ComBom = new(encoderShouldEmitUTF8Identifier: true);

        private static readonly char[] CaracteresQueExigemAspas = { ';', '"', '\n', '\r' };

        public static async Task<int> EscreverAsync(
            IAsyncEnumerable<AgendamentoRelatorioLinha> linhas,
            Stream destino,
            CancellationToken token = default)
        {
            await using var writer = new StreamWriter(destino, Utf8ComBom, bufferSize: 64 * 1024, leaveOpen: true);
            writer.NewLine = "\r\n";

            await writer.WriteLineAsync(Cabecalho);

            var total = 0;
            await foreach (var linha in linhas.WithCancellation(token))
            {
                await writer.WriteLineAsync(FormatarLinha(linha));
                total++;
            }

            await writer.FlushAsync(token);
            return total;
        }

        private static string FormatarLinha(AgendamentoRelatorioLinha a) =>
            string.Join(';',
                Campo(a.FornecedorNome),
                Campo(a.Produto),
                Campo(a.PlacaVeiculo),
                Campo(a.MotoristaNome),
                Campo(a.UnidadeEntrega),
                Campo(a.LocalDescarga),
                Campo(RelatorioDetalhadoCalculo.TipoVeiculoRotulo(a.TipoVeiculo)),
                a.PesoCarga?.ToString("0.##", RelatorioFormatacao.PtBr) ?? "",
                Campo(RelatorioDetalhadoCalculo.StatusRotulo(a.Status)),
                RelatorioFormatacao.DataHoraLocal(a.DataInicio),
                RelatorioFormatacao.DataHoraLocal(a.DataFim),
                a.DaGrade ? "Grade" : "Avulso",
                a.NotaFiscalNumero?.ToString(RelatorioFormatacao.PtBr) ?? "",
                DataHora(a.ChegouEm),
                Campo(RelatorioDetalhadoCalculo.SituacaoRotulo(
                    RelatorioDetalhadoCalculo.Situacao(a.Status, a.DataInicio, a.DataFim, a.ChegouEm))),
                DataHora(a.Ciclo?.InicioDescarga),
                a.Ciclo?.InicioDescarga is null ? "" : a.Ciclo.InicioEstimado ? "Sim" : "Não",
                DataHora(a.FimDescargaEm),
                DataHora(a.Ciclo?.Saida),
                Minutos(a.Ciclo?.EsperaMinutos),
                Minutos(a.Ciclo?.DescargaMinutos),
                Minutos(a.Ciclo?.PermanenciaMinutos));

        private static string DataHora(DateTime? utc) =>
            utc.HasValue ? RelatorioFormatacao.DataHoraLocal(utc.Value) : "";

        private static string Minutos(double? minutos) =>
            minutos.HasValue ? Math.Round(minutos.Value).ToString("0", RelatorioFormatacao.PtBr) : "";

        private static string Campo(string? valor)
        {
            if (string.IsNullOrEmpty(valor))
            {
                return "";
            }

            if (valor[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            {
                valor = "'" + valor;
            }

            if (valor.IndexOfAny(CaracteresQueExigemAspas) >= 0)
            {
                return $"\"{valor.Replace("\"", "\"\"")}\"";
            }

            return valor;
        }
    }
}
