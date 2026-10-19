using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
using static TruckFlow.Test.Relatorios.RelatorioTestData;

namespace TruckFlow.Test.Relatorios
{
    public class RelatorioCsvWriterTests
    {
        private static async Task<(byte[] Bytes, int Total)> Escrever(params AgendamentoRelatorioLinha[] linhas)
        {
            using var destino = new MemoryStream();
            var total = await RelatorioCsvWriter.EscreverAsync(linhas.ToAsyncEnumerable(), destino);
            return (destino.ToArray(), total);
        }

        private static string[] Linhas(byte[] bytes) =>
            LerTexto(bytes).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        [Fact]
        public async Task Comeca_com_BOM_UTF8()
        {
            var (bytes, _) = await Escrever();

            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
        }

        [Fact]
        public async Task Sem_linhas_escreve_so_o_cabecalho_e_retorna_zero()
        {
            var (bytes, total) = await Escrever();

            Assert.Equal(0, total);
            Assert.Equal(new[] { RelatorioCsvWriter.Cabecalho }, Linhas(bytes));
        }

        [Fact]
        public async Task Formata_linha_completa_em_pt_BR_e_horario_local()
        {
            var (bytes, total) = await Escrever(Linha());

            Assert.Equal(1, total);
            Assert.Equal(
                "Fornecedor A;Soja;ABC1D23;João Silva;Unidade 1;Doca 3;Ls;1500,5;Agendado;01/10/2026 07:30;01/10/2026 08:30",
                Linhas(bytes)[1]);
        }

        [Fact]
        public async Task Campos_nulos_ficam_vazios_e_produto_cai_pro_tipo_de_carga()
        {
            var (bytes, _) = await Escrever(Linha(fornecedor: null, produto: null, placa: null, peso: null));

            var colunas = Linhas(bytes)[1].Split(';');
            Assert.Equal("", colunas[0]);      // fornecedor
            Assert.Equal("Milho", colunas[1]); // produto -> TipoCarga
            Assert.Equal("", colunas[2]);      // placa
            Assert.Equal("", colunas[7]);      // peso
        }

        [Fact]
        public async Task Escapa_ponto_e_virgula_e_aspas()
        {
            var (bytes, _) = await Escrever(Linha(fornecedor: "Silva; Filhos \"ME\""));

            Assert.StartsWith("\"Silva; Filhos \"\"ME\"\"\";", Linhas(bytes)[1]);
        }

        [Fact]
        public async Task Quebra_de_linha_dentro_do_campo_fica_entre_aspas()
        {
            var (bytes, _) = await Escrever(Linha(fornecedor: "Linha 1\nLinha 2"));

            Assert.StartsWith("\"Linha 1\nLinha 2\";", LerTexto(bytes).Split("\r\n")[1]);
        }

        [Theory]
        [InlineData("=1+1")]
        [InlineData("+5511999")]
        [InlineData("-10")]
        [InlineData("@SUM(A1)")]
        public async Task Protege_contra_CSV_injection(string valorMalicioso)
        {
            var (bytes, _) = await Escrever(Linha(placa: valorMalicioso));

            var placa = Linhas(bytes)[1].Split(';')[2];
            Assert.Equal("'" + valorMalicioso, placa);
        }

        [Fact]
        public async Task Formula_com_aspas_recebe_apostrofo_e_fica_entre_aspas()
        {
            var (bytes, _) = await Escrever(Linha(placa: "=HYPERLINK(\"http://x\")"));

            Assert.Contains(";\"'=HYPERLINK(\"\"http://x\"\")\";", Linhas(bytes)[1]);
        }

        [Fact]
        public async Task Usa_CRLF_e_retorna_quantidade_de_linhas()
        {
            var (bytes, total) = await Escrever(Linha(), Linha(), Linha());

            Assert.Equal(3, total);
            var texto = LerTexto(bytes);
            Assert.Equal(4, texto.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).Length);
            Assert.DoesNotContain("\r\n\n", texto);
        }

        [Fact]
        public async Task Nao_fecha_o_stream_de_destino()
        {
            using var destino = new MemoryStream();

            await RelatorioCsvWriter.EscreverAsync(new[] { Linha() }.ToAsyncEnumerable(), destino);

            Assert.True(destino.CanWrite);
        }

        [Fact]
        public async Task Respeita_cancelamento()
        {
            using var destino = new MemoryStream();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RelatorioCsvWriter.EscreverAsync(new[] { Linha() }.ToAsyncEnumerable(), destino, cts.Token));
        }
    }
}
