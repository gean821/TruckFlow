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
            "Fornecedor;Produto;Placa;Motorista;Unidade;Local;TipoVeiculo;Peso;Status;DataInicio;DataFim";

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
                Campo(a.TipoVeiculo?.ToString()),
                a.PesoCarga?.ToString("0.##", RelatorioFormatacao.PtBr) ?? "",
                Campo(a.Status.ToString()),
                RelatorioFormatacao.DataHoraLocal(a.DataInicio),
                RelatorioFormatacao.DataHoraLocal(a.DataFim));

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
