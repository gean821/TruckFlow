using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Events;
using TruckFlow.Extensions.Notificacao;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Database.Interceptors;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Test.Integration.Fixtures;

internal static class DispatcherTestHelper
{
    public static ServiceProvider BuildServiceProvider(DatabaseFixture fixture)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHttpContextAccessor();
        services.AddScoped<IEmpresaContext, EmpresaContext>();
        services.AddSingleton<IOutboxEventTypeResolver, OutboxEventTypeResolver>();

        services.AddScoped<IDomainEventHandler<AgendamentoCanceladoEvent>, AgendamentoCanceladoNotificacaoHandler>();
        services.AddScoped<IDomainEventHandler<AgendamentoEvent.MotoristaChegouEvent>, MotoristaChegouNotificacaoHandler>();
        services.AddScoped<IDomainEventHandler<AgendamentoEvent.AgendamentoReagendadoEvent>, AgendamentoReagendadoNotificacaoHandler>();
        services.AddScoped<IDomainEventHandler<AgendamentoEvent.AgendamentoConfirmadoEvent>, AgendamentoConfirmadoNotificacaoHandler>();
        services.AddScoped<IDomainEventHandler<AgendamentoEvent.AgendamentoExpiradoEvent>, AgendamentoExpiradoNotificacaoHandler>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options
                .UseNpgsql(fixture.ConnectionString)
                .AddInterceptors(
                    new EmpresaScopedSaveChangesInterceptor(sp.GetRequiredService<IEmpresaContext>()),
                    new OutboxSaveChangesInterceptor()));

        return services.BuildServiceProvider();
    }

    public static OutboxProcessorWorker CreateWorker(DatabaseFixture fixture, out ServiceProvider serviceProvider)
    {
        serviceProvider = BuildServiceProvider(fixture);
        return new OutboxProcessorWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessorWorker>.Instance);
    }

    public static async Task EsperarTodosProcessadosAsync(
        DatabaseFixture fixture,
        TimeSpan? timeout = null)
    {
        var limite = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));

        while (DateTime.UtcNow < limite)
        {
            using var db = fixture.CreateDbContext();
            var pendentes = await db.OutboxEvent
                .IgnoreQueryFilters()
                .CountAsync(e => e.ProcessedAt == null);

            if (pendentes == 0)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException("OutboxEvent pendente não foi processado dentro do timeout do teste.");
    }
}
