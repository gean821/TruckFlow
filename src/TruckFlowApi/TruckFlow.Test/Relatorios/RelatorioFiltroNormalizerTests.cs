using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;

namespace TruckFlow.Test.Relatorios
{
    public class RelatorioFiltroNormalizerTests
    {
        private static RelatorioAgendamentoFilterDto Filtro(DateTime? inicio = null, DateTime? fim = null) =>
            new()
            {
                DataInicio = inicio ?? new DateTime(2026, 10, 1),
                DataFim = fim ?? new DateTime(2026, 10, 31)
            };

        [Fact]
        public void Sem_periodo_lanca_BusinessException()
        {
            var filtro = new RelatorioAgendamentoFilterDto { DataInicio = new DateTime(2026, 10, 1) };

            var ex = Assert.Throws<BusinessException>(() => RelatorioFiltroNormalizer.Normalizar(filtro));
            Assert.Contains("Informe o período", ex.Message);
        }

        [Fact]
        public void DataFim_antes_da_DataInicio_lanca_BusinessException()
        {
            var filtro = Filtro(new DateTime(2026, 10, 10), new DateTime(2026, 10, 9));

            var ex = Assert.Throws<BusinessException>(() => RelatorioFiltroNormalizer.Normalizar(filtro));
            Assert.Contains("data fim não pode ser anterior", ex.Message);
        }

        [Fact]
        public void Mesmo_dia_no_inicio_e_no_fim_e_aceito()
        {
            var filtro = Filtro(new DateTime(2026, 10, 1), new DateTime(2026, 10, 1));

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.True(criteria.DataFimUtc > criteria.DataInicioUtc);
        }

        [Fact]
        public void Periodo_de_366_dias_e_aceito()
        {
            // 2024 é bissexto: 01/01 a 31/12 = exatamente 366 dias
            var filtro = Filtro(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.NotNull(criteria);
        }

        [Fact]
        public void Periodo_acima_de_366_dias_lanca_BusinessException()
        {
            var filtro = Filtro(new DateTime(2026, 1, 1), new DateTime(2027, 1, 2)); // 367 dias

            var ex = Assert.Throws<BusinessException>(() => RelatorioFiltroNormalizer.Normalizar(filtro));
            Assert.Contains("período máximo", ex.Message);
        }

        [Fact]
        public void HoraInicio_maior_que_HoraFim_lanca_BusinessException()
        {
            var filtro = Filtro();
            filtro.HoraInicio = new TimeSpan(14, 0, 0);
            filtro.HoraFim = new TimeSpan(8, 0, 0);

            var ex = Assert.Throws<BusinessException>(() => RelatorioFiltroNormalizer.Normalizar(filtro));
            Assert.Contains("hora início não pode ser maior", ex.Message);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(24)]
        public void Hora_fora_do_dia_lanca_BusinessException(int horas)
        {
            var filtro = Filtro();
            filtro.HoraInicio = TimeSpan.FromHours(horas);

            Assert.Throws<BusinessException>(() => RelatorioFiltroNormalizer.Normalizar(filtro));
        }

        [Fact]
        public void Inicio_do_periodo_vira_meia_noite_de_Sao_Paulo_em_UTC()
        {
            var criteria = RelatorioFiltroNormalizer.Normalizar(Filtro());

            // 01/10 00:00 em SP (UTC-3) = 01/10 03:00 UTC
            Assert.Equal(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc), criteria.DataInicioUtc);
            Assert.Equal(DateTimeKind.Utc, criteria.DataInicioUtc.Kind);
        }

        [Fact]
        public void Fim_do_periodo_cobre_o_dia_inteiro_em_Sao_Paulo()
        {
            var criteria = RelatorioFiltroNormalizer.Normalizar(Filtro());

            // 31/10 23:59:59.9999999 em SP = 01/11 02:59:59.9999999 UTC
            var esperado = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc).AddTicks(-1);
            Assert.Equal(esperado, criteria.DataFimUtc);
        }

        [Fact]
        public void Horario_que_venha_junto_com_a_data_e_ignorado()
        {
            var filtro = Filtro(inicio: new DateTime(2026, 10, 1, 15, 30, 0));

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.Equal(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc), criteria.DataInicioUtc);
        }

        [Fact]
        public void Repassa_filtros_de_hora_fornecedor_e_produto()
        {
            var fornecedorId = Guid.NewGuid();
            var produtoId = Guid.NewGuid();
            var filtro = Filtro();
            filtro.HoraInicio = new TimeSpan(8, 0, 0);
            filtro.HoraFim = new TimeSpan(12, 0, 0);
            filtro.FornecedorId = fornecedorId;
            filtro.ProdutoId = produtoId;

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.Equal(new TimeSpan(8, 0, 0), criteria.HoraInicio);
            Assert.Equal(new TimeSpan(12, 0, 0), criteria.HoraFim);
            Assert.Equal(fornecedorId, criteria.FornecedorId);
            Assert.Equal(produtoId, criteria.ProdutoId);
        }

        [Fact]
        public void Placa_e_motorista_sao_aparados_e_vazios_viram_null()
        {
            var filtro = Filtro();
            filtro.PlacaVeiculo = "  abc1  ";
            filtro.Motorista = "   ";

            var criteria = RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.Equal("abc1", criteria.PlacaVeiculo);
            Assert.Null(criteria.Motorista);
        }

        [Fact]
        public void Nao_altera_o_dto_recebido()
        {
            var filtro = Filtro();

            RelatorioFiltroNormalizer.Normalizar(filtro);

            Assert.Equal(new DateTime(2026, 10, 1), filtro.DataInicio);
            Assert.Equal(new DateTime(2026, 10, 31), filtro.DataFim);
        }
    }
}
