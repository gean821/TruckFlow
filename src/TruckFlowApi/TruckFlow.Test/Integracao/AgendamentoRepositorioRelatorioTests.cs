using TruckFlow.Application.Relatorios;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlowApi.Infra.Repositories;
using static TruckFlow.Test.Integracao.PostgresRelatorioFixture;

namespace TruckFlow.Test.Integracao
{
    /// <summary>
    /// Roda as consultas do relatório contra um Postgres real, provando que o filtro de hora
    /// (AT TIME ZONE), a busca com ILIKE e o filtro de tenant são traduzidos e executados no banco.
    /// Precisa do Docker rodando. Para pular: dotnet test --filter "Category!=Integracao"
    /// </summary>
    [Trait("Category", "Integracao")]
    public class AgendamentoRepositorioRelatorioTests : IClassFixture<PostgresRelatorioFixture>
    {
        private readonly PostgresRelatorioFixture _fixture;

        public AgendamentoRepositorioRelatorioTests(PostgresRelatorioFixture fixture)
        {
            _fixture = fixture;
        }

        /// <summary>Monta o criteria do jeito que o service faz: passando pelo normalizer.</summary>
        private static RelatorioAgendamentoCriteria Dia01DeOutubro(Action<RelatorioAgendamentoFilterDto>? ajustar = null)
        {
            var filtro = new RelatorioAgendamentoFilterDto
            {
                DataInicio = new DateTime(2026, 10, 1),
                DataFim = new DateTime(2026, 10, 1)
            };
            ajustar?.Invoke(filtro);
            return RelatorioFiltroNormalizer.Normalizar(filtro);
        }

        private async Task<List<AgendamentoRelatorioLinha>> Buscar(
            RelatorioAgendamentoCriteria criteria,
            Guid? empresa = null,
            int limite = 100)
        {
            await using var db = _fixture.CriarContexto(empresa ?? _fixture.EmpresaA);
            var repo = new AgendamentoRepositorio(db);
            return await repo.GetLinhasRelatorioAsync(criteria, limite);
        }

        // ============= Período e tenant =============

        [Fact]
        public async Task Periodo_considera_o_dia_em_Sao_Paulo_e_nao_o_dia_UTC()
        {
            var linhas = await Buscar(Dia01DeOutubro());

            // R3 (22:30 local, já 02/10 em UTC) entra; R4 (23:30 do dia 30/09 local, já 01/10 em UTC) fica fora
            Assert.Equal(new[] { R1Inicio, R2Inicio, R3Inicio }, linhas.Select(l => l.DataInicio));
        }

        [Fact]
        public async Task Cada_empresa_ve_somente_os_proprios_agendamentos()
        {
            var daEmpresaB = await Buscar(Dia01DeOutubro(), empresa: _fixture.EmpresaB);

            var linha = Assert.Single(daEmpresaB);
            Assert.Equal(R5Inicio, linha.DataInicio);
        }

        [Fact]
        public async Task Count_bate_com_a_quantidade_de_linhas()
        {
            await using var db = _fixture.CriarContexto(_fixture.EmpresaA);
            var repo = new AgendamentoRepositorio(db);

            var total = await repo.CountLinhasRelatorioAsync(Dia01DeOutubro());

            Assert.Equal(3, total);
        }

        // ============= Filtro de hora (AT TIME ZONE) =============

        [Fact]
        public async Task Filtro_de_hora_usa_o_horario_de_Sao_Paulo()
        {
            var linhas = await Buscar(Dia01DeOutubro(f =>
            {
                f.HoraInicio = new TimeSpan(7, 0, 0);
                f.HoraFim = new TimeSpan(8, 0, 0);
            }));

            // R1 é 07:30 em SP (10:30 em UTC). Se o filtro comparasse em UTC, não viria nada.
            var linha = Assert.Single(linhas);
            Assert.Equal(R1Inicio, linha.DataInicio);
        }

        [Fact]
        public async Task Filtro_de_hora_funciona_quando_o_UTC_ja_virou_o_dia()
        {
            var linhas = await Buscar(Dia01DeOutubro(f => f.HoraInicio = new TimeSpan(22, 0, 0)));

            // R3 é 22:30 em SP, mas 01:30 em UTC: só aparece se a hora for convertida no banco
            var linha = Assert.Single(linhas);
            Assert.Equal(R3Inicio, linha.DataInicio);
        }

        [Fact]
        public async Task Hora_fim_e_inclusiva()
        {
            var linhas = await Buscar(Dia01DeOutubro(f =>
            {
                f.HoraInicio = new TimeSpan(13, 0, 0);
                f.HoraFim = new TimeSpan(13, 0, 0);
            }));

            Assert.Equal(R2Inicio, Assert.Single(linhas).DataInicio);
        }

        // ============= Busca de placa (ILIKE) =============

        [Fact]
        public async Task Busca_de_placa_ignora_maiusculas_e_minusculas()
        {
            var linhas = await Buscar(Dia01DeOutubro(f => f.PlacaVeiculo = "abc1"));

            Assert.Equal("ABC1D23", Assert.Single(linhas).PlacaVeiculo);
        }

        [Fact]
        public async Task Busca_de_placa_encontra_trecho_no_meio()
        {
            var linhas = await Buscar(Dia01DeOutubro(f => f.PlacaVeiculo = "9k8"));

            Assert.Equal("XYZ9K88", Assert.Single(linhas).PlacaVeiculo);
        }

        [Fact]
        public async Task Underscore_digitado_e_tratado_como_texto_e_nao_como_curinga()
        {
            // Sem escape, "B_1" casaria com "ABC1D23" (o _ valeria qualquer caractere)
            var linhas = await Buscar(Dia01DeOutubro(f => f.PlacaVeiculo = "B_1"));

            Assert.Equal("AB_1234", Assert.Single(linhas).PlacaVeiculo);
        }

        [Fact]
        public async Task Percentual_digitado_e_tratado_como_texto()
        {
            var linhas = await Buscar(Dia01DeOutubro(f => f.PlacaVeiculo = "%"));

            Assert.Empty(linhas);
        }

        // ============= Projeção, ordem e limite =============

        [Fact]
        public async Task Projecao_preenche_local_de_descarga_e_peso()
        {
            var r1 = (await Buscar(Dia01DeOutubro())).First();

            Assert.Equal("Doca 1", r1.LocalDescarga);
            Assert.Equal("Unidade Teste", r1.UnidadeEntrega);
            Assert.Equal(1500m, r1.PesoCarga);
            Assert.Equal("Milho", r1.Produto); // sem produto/grade -> cai pro tipo de carga
        }

        [Fact]
        public async Task Limite_corta_mantendo_a_ordem_por_data()
        {
            var linhas = await Buscar(Dia01DeOutubro(), limite: 2);

            Assert.Equal(new[] { R1Inicio, R2Inicio }, linhas.Select(l => l.DataInicio));
        }

        [Fact]
        public async Task Stream_retorna_as_mesmas_linhas_que_a_lista()
        {
            await using var db = _fixture.CriarContexto(_fixture.EmpresaA);
            var repo = new AgendamentoRepositorio(db);
            var criteria = Dia01DeOutubro();

            var viaStream = new List<DateTime>();
            await foreach (var linha in repo.StreamLinhasRelatorioAsync(criteria, RelatorioLimites.MaxLinhasCsv))
            {
                viaStream.Add(linha.DataInicio);
            }

            Assert.Equal(new[] { R1Inicio, R2Inicio, R3Inicio }, viaStream);
        }
    }
}
