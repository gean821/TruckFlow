using System.Runtime.CompilerServices;
using System.Text;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Test.Relatorios
{
    internal static class RelatorioTestData
    {
        /// <summary>Converte um horário de São Paulo para UTC (como o banco guarda).</summary>
        public static DateTime LocalParaUtc(int ano, int mes, int dia, int hora, int minuto) =>
            TimeZoneInfo.ConvertTimeToUtc(
                new DateTime(ano, mes, dia, hora, minuto, 0, DateTimeKind.Unspecified),
                Grade.OperationalTimeZone);

        public static AgendamentoRelatorioLinha Linha(
            string? fornecedor = "Fornecedor A",
            string? produto = "Soja",
            string? placa = "ABC1D23",
            decimal? peso = 1500.5m) =>
            new()
            {
                FornecedorNome = fornecedor,
                ProdutoNome = produto,
                TipoCarga = TipoCarga.Milho,
                PlacaVeiculo = placa,
                MotoristaNome = "João Silva",
                UnidadeEntrega = "Unidade 1",
                LocalDescarga = "Doca 3",
                TipoVeiculo = TipoVeiculo.Ls,
                PesoCarga = peso,
                Status = StatusAgendamento.Agendado,
                DataInicio = LocalParaUtc(2026, 10, 1, 7, 30),
                DataFim = LocalParaUtc(2026, 10, 1, 8, 30)
            };

        /// <summary>Imita o AsAsyncEnumerable do EF: entrega item a item e respeita cancelamento.</summary>
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
            this IEnumerable<T> itens,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            foreach (var item in itens)
            {
                token.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return item;
            }
        }

        /// <summary>Lê o CSV gerado removendo o BOM (o StreamReader detecta e descarta).</summary>
        public static string LerTexto(byte[] bytes)
        {
            using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return reader.ReadToEnd();
        }
    }
}
