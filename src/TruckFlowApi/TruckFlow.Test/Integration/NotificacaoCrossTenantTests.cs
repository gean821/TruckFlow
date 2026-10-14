using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Repositories;
using TruckFlow.Test.Integration.Fakes;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class NotificacaoCrossTenantTests : IntegrationTestBase
{
    public NotificacaoCrossTenantTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CancelarAgendamentoEmEmpresaA_NaoVazaNotificacaoParaUsuarioDeEmpresaB()
    {
        var empresaA = TestDataSeeder.NovaEmpresa("Empresa A");
        var empresaB = TestDataSeeder.NovaEmpresa("Empresa B");
        var empresaAId = empresaA.Id;
        var empresaBId = empresaB.Id;

        Guid agendamentoId, motoristaAId, usuarioBId;

        var setupEmpresaContext = ServiceFactory.NewEmpresaContext();
        using (setupEmpresaContext.WithTenant(empresaAId))
        using (var setupDb = Fixture.CreateDbContext(setupEmpresaContext))
        {
            setupDb.Empresa.AddRange(empresaA, empresaB);
            await setupDb.SaveChangesAsync();

            var unidadeA = TestDataSeeder.NovaUnidadeEntrega(empresaAId);
            setupDb.UnidadeEntrega.Add(unidadeA);
            await setupDb.SaveChangesAsync();

            var motoristaA = TestDataSeeder.NovoUsuarioMotorista(empresaAId);
            var usuarioB = TestDataSeeder.NovoUsuarioMotorista(empresaBId);
            setupDb.Usuario.AddRange(motoristaA, usuarioB);
            motoristaAId = motoristaA.Id;
            usuarioBId = usuarioB.Id;

            var vaga = TestDataSeeder.NovaVagaDisponivel(empresaAId, unidadeA.Id, Guid.NewGuid());
            vaga.ProdutoId = null;
            vaga.StatusAgendamento = TruckFlow.Domain.Enums.StatusAgendamento.Agendado;
            vaga.UsuarioId = motoristaAId;
            setupDb.Agendamento.Add(vaga);
            await setupDb.SaveChangesAsync();
            agendamentoId = vaga.Id;
        }

        var cancelEmpresaContext = ServiceFactory.NewEmpresaContext();
        using (cancelEmpresaContext.WithTenant(empresaAId))
        using (var cancelDb = Fixture.CreateDbContext(cancelEmpresaContext))
        {
            var repo = new AgendamentoRepositorio(cancelDb);
            var vaga = await repo.GetByIdAcrossTenants(agendamentoId);
            vaga!.Cancelar("Teste de isolamento cross-tenant");
            await repo.Update(vaga);
        }

        using (var dispatchDb = Fixture.CreateDbContext())
        {
            var pendente = await dispatchDb.OutboxEvent
                .IgnoreQueryFilters()
                .SingleAsync(e => e.ProcessedAt == null);

            Assert.Equal(empresaAId, pendente.EmpresaId);
        }

        var worker = DispatcherTestHelper.CreateWorker(Fixture, out var dispatcherServices);
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await DispatcherTestHelper.EsperarTodosProcessadosAsync(Fixture);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            await dispatcherServices.DisposeAsync();
        }

        Guid notificacaoId;
        using (var rawCheckDb = Fixture.CreateDbContext())
        {
            var notificacao = await rawCheckDb.Notificacao.IgnoreQueryFilters().SingleAsync();
            Assert.Equal(empresaAId, notificacao.EmpresaId);
            Assert.Equal(motoristaAId, notificacao.DestinatarioUsuarioId);
            notificacaoId = notificacao.Id;
        }

        var empresaContextB = ServiceFactory.NewEmpresaContext();
        using var tenantScopeB = empresaContextB.WithTenant(empresaBId);
        using var checkDbAsB = Fixture.CreateDbContext(empresaContextB);

        var currentUserB = new FakeCurrentUserService
        {
            UserId = usuarioBId,
            EmpresaId = empresaBId,
            IsMotorista = false,
        };

        var notificacaoRepo = new NotificacaoRepositorio(checkDbAsB);
        var notificacaoService = new NotificacaoService(notificacaoRepo, currentUserB, empresaContextB);

        var listaDeB = await notificacaoService.ListarMinhasAsync(
            new NotificacaoListQueryDto(), CancellationToken.None);

        Assert.Empty(listaDeB.Items);

        var marcouComoLida = await notificacaoService.MarcarComoLidaAsync(
            notificacaoId, CancellationToken.None);

        Assert.False(marcouComoLida);
    }
}
