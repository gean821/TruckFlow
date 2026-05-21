using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Extensions.Notificacao
{
    public static class NotificacaoDependencyInjection
    {
        public static IServiceCollection AddNotificacao(this IServiceCollection services)
        {
            services.AddSingleton<IOutboxEventTypeResolver, OutboxEventTypeResolver>();

            services.AddScoped<
                IDomainEventHandler<AgendamentoCanceladoEvent>,
                AgendamentoCanceladoNotificacaoHandler>();

            services.AddHostedService<OutboxProcessorWorker>();

            return services;
        }
    }
}