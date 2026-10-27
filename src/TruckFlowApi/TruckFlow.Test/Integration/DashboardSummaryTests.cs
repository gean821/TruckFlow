using Microsoft.Extensions.Time.Testing;
using TruckFlow.Application;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;
using TruckFlowApi.Infra.Repositories;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class DashboardSummaryTests : IntegrationTestBase
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 20, 2, 30, 0, TimeSpan.Zero);

    public DashboardSummaryTests(DatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Docas_ContaSoAtivasEOcupadaPorAgendamentoEmAndamento()
    {
        var cenario = await CriarCenarioAsync(configuracoes: null, (empresaId, unidade, docaA, docaB, docaInativa, produtoId) =>
        [
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                Agora.UtcDateTime.AddHours(-1), Agora.UtcDateTime.AddHours(1), StatusAgendamento.EmAndamento),
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaInativa.Id, unidade.Id, produtoId,
                Agora.UtcDateTime.AddHours(-1), Agora.UtcDateTime.AddHours(1), StatusAgendamento.EmAndamento),
        ]);

        var resumo = await cenario();

        Assert.Equal(2, resumo.Docas.Total);
        Assert.Equal(1, resumo.Docas.Ocupadas);
        Assert.Equal(1, resumo.Docas.Livres);
        Assert.Equal(50, resumo.Docas.OcupacaoPorcentagem);
    }

    [Fact]
    public async Task Dia_ViraAMeiaNoiteDeBrasiliaENaoEmUtc()
    {
        var cenario = await CriarCenarioAsync(configuracoes: null, (empresaId, unidade, docaA, docaB, docaInativa, produtoId) =>
        [
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 20, 1, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 20, 1, 30, 0, DateTimeKind.Utc)),
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 20, 4, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 20, 4, 30, 0, DateTimeKind.Utc)),
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 19, 2, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 19, 2, 30, 0, DateTimeKind.Utc)),
        ]);

        var resumo = await cenario();

        Assert.Equal(1, resumo.Stats.TotalAgendamentos);
    }

    [Fact]
    public async Task TotalDoDia_NaoContaVagaVaziaDaGrade()
    {
        var cenario = await CriarCenarioAsync(configuracoes: null, (empresaId, unidade, docaA, docaB, docaInativa, produtoId) =>
        [
            TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 19, 16, 0, 0, DateTimeKind.Utc)),
            TestDataSeeder.NovaVagaDisponivel(empresaId, unidade.Id, produtoId, new DateTime(2026, 10, 19, 17, 0, 0, DateTimeKind.Utc)),
            TestDataSeeder.NovaVagaDisponivel(empresaId, unidade.Id, produtoId, new DateTime(2026, 10, 19, 19, 0, 0, DateTimeKind.Utc)),
        ]);

        var resumo = await cenario();

        Assert.Equal(1, resumo.Stats.TotalAgendamentos);
    }

    [Fact]
    public async Task Meta_VemDaConfiguracaoDaEmpresa()
    {
        var cenario = await CriarCenarioAsync("""{"metaDiariaKg":1000}""", (empresaId, unidade, docaA, docaB, docaInativa, produtoId) =>
        [
            ComVolume(TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 19, 16, 0, 0, DateTimeKind.Utc)), 250m),
        ]);

        var resumo = await cenario();

        Assert.Equal(250m, resumo.Volume.TotalKg);
        Assert.Equal(1000m, resumo.Volume.MetaDiariaKg);
        Assert.Equal(25, resumo.Volume.ProgressoDiario);
    }

    [Fact]
    public async Task Meta_SemConfiguracao_NaoInventaMetaEProgressoFicaZero()
    {
        var cenario = await CriarCenarioAsync(configuracoes: null, (empresaId, unidade, docaA, docaB, docaInativa, produtoId) =>
        [
            ComVolume(TestDataSeeder.NovoAgendamentoAtivoNaDoca(empresaId, docaA.Id, unidade.Id, produtoId,
                new DateTime(2026, 10, 19, 15, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 19, 16, 0, 0, DateTimeKind.Utc)), 250m),
        ]);

        var resumo = await cenario();

        Assert.Equal(250m, resumo.Volume.TotalKg);
        Assert.Null(resumo.Volume.MetaDiariaKg);
        Assert.Equal(0, resumo.Volume.ProgressoDiario);
    }

    private static Agendamento ComVolume(Agendamento agendamento, decimal volume)
    {
        agendamento.VolumeCarga = volume;
        return agendamento;
    }

    private async Task<Func<Task<DashboardResponseDto>>> CriarCenarioAsync(
        string? configuracoes,
        Func<Guid, UnidadeEntrega, LocalDescarga, LocalDescarga, LocalDescarga, Guid, Agendamento[]> agendamentos)
    {
        var empresa = TestDataSeeder.NovaEmpresa($"Empresa Dashboard {Guid.NewGuid():N}");
        empresa.Configuracoes = configuracoes;

        var unidade = TestDataSeeder.NovaUnidadeEntrega(empresa.Id);
        var docaA = TestDataSeeder.NovoLocalDescarga(empresa.Id, unidade.Id, unidade);
        var docaB = TestDataSeeder.NovoLocalDescarga(empresa.Id, unidade.Id, unidade);
        docaB.Nome = "Doca 2";
        var docaInativa = TestDataSeeder.NovoLocalDescarga(empresa.Id, unidade.Id, unidade);
        docaInativa.Nome = "Doca Inativa";
        docaInativa.Ativa = false;
        var produto = TestDataSeeder.NovoProduto(empresa.Id, docaA.Id, docaA);

        var empresaContext = ServiceFactory.NewEmpresaContext();

        using (var setupDb = Fixture.CreateDbContext())
        {
            setupDb.Empresa.Add(empresa);
            await setupDb.SaveChangesAsync();
        }

        using (empresaContext.WithTenant(empresa.Id))
        using (var setupDb = Fixture.CreateDbContext(empresaContext))
        {
            setupDb.UnidadeEntrega.Add(unidade);
            setupDb.LocalDescarga.AddRange(docaA, docaB, docaInativa);
            setupDb.Produto.Add(produto);
            await setupDb.SaveChangesAsync();

            setupDb.Agendamento.AddRange(agendamentos(empresa.Id, unidade, docaA, docaB, docaInativa, produto.Id));
            await setupDb.SaveChangesAsync();
        }

        return async () =>
        {
            using var tenant = empresaContext.WithTenant(empresa.Id);
            using var db = Fixture.CreateDbContext(empresaContext);

            var service = new DashboardService(
                new DashboardRepositorio(db),
                new EmpresaRepositorio(db),
                new CurrentUserGuard(new FakeCurrentUserService { EmpresaId = empresa.Id, IsMotorista = false }),
                new FakeTimeProvider(Agora));

            return await service.GetDashboardSummaryAsync();
        };
    }
}
