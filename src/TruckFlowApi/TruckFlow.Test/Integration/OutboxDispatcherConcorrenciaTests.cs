using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Events;
using TruckFlow.Test.Integration.Fixtures;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class OutboxDispatcherConcorrenciaTests : IntegrationTestBase
{
    public OutboxDispatcherConcorrenciaTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task DoisWorkersConcorrentes_NaoProcessamOMesmoEventoDuasVezes()
    {
        Guid empresaId, outboxEventId, motoristaId;

        using (var setupDb = Fixture.CreateDbContext())
        {
            var empresa = TestDataSeeder.NovaEmpresa("Empresa Dispatcher");
            setupDb.Empresa.Add(empresa);
            await setupDb.SaveChangesAsync();
            empresaId = empresa.Id;

            var motorista = TestDataSeeder.NovoUsuarioMotorista(empresaId);
            setupDb.Usuario.Add(motorista);
            await setupDb.SaveChangesAsync();
            motoristaId = motorista.Id;

            var domainEvent = new AgendamentoCanceladoEvent(
                AgendamentoId: Guid.NewGuid(),
                EmpresaId: empresaId,
                MotoristaUsuarioId: motoristaId,
                MotivoCancelamento: "Teste de concorrência do dispatcher",
                OcorridoEm: DateTime.UtcNow);

            var outboxEvent = new OutboxEvent
            {
                EventType = domainEvent.GetType().FullName!,
                Payload = OutboxEventSerializer.Serialize(domainEvent),
                IdempotencyKey = domainEvent.IdempotencyKey,
                EmpresaId = empresaId,
                OcorridoEm = domainEvent.OcorridoEm,
                CreatedAt = DateTime.UtcNow,
            };
            setupDb.OutboxEvent.Add(outboxEvent);
            await setupDb.SaveChangesAsync();
            outboxEventId = outboxEvent.Id;
        }

        var worker1 = DispatcherTestHelper.CreateWorker(Fixture, out var services1);
        var worker2 = DispatcherTestHelper.CreateWorker(Fixture, out var services2);

        try
        {
            await Task.WhenAll(
                worker1.StartAsync(CancellationToken.None),
                worker2.StartAsync(CancellationToken.None));

            await DispatcherTestHelper.EsperarTodosProcessadosAsync(Fixture);
        }
        finally
        {
            await Task.WhenAll(
                worker1.StopAsync(CancellationToken.None),
                worker2.StopAsync(CancellationToken.None));

            await services1.DisposeAsync();
            await services2.DisposeAsync();
        }

        using var assertDb = Fixture.CreateDbContext();

        var eventoFinal = await assertDb.OutboxEvent.IgnoreQueryFilters()
            .SingleAsync(e => e.Id == outboxEventId);
        Assert.NotNull(eventoFinal.ProcessedAt);
        Assert.Equal(0, eventoFinal.Tentativas);

        var notificacoes = await assertDb.Notificacao.IgnoreQueryFilters()
            .Where(n => n.DestinatarioUsuarioId == motoristaId)
            .ToListAsync();

        Assert.Single(notificacoes);
    }
}
