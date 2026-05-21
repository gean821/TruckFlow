using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database.Interceptors;
using TruckFlowApi.Infra.Outbox;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Notificacao
{
    public static class NotificacaoDependencyInjection
    {
        public static IServiceCollection AddNotificacao(this IServiceCollection services)
        {
            services.AddSingleton<IOutboxEventTypeResolver, OutboxEventTypeResolver>();
            services.AddSingleton<INotificationConnectionManager, NotificationConnectionManager>();
            services.AddSingleton<SseNotificationStreamer>();
            services.AddScoped<RealtimeNotificationInterceptor>();

            services.AddTransient<INotificacaoRepositorio, NotificacaoRepositorio>();
            services.AddTransient<IOutboxEventRepositorio, OutboxEventRepositorio>();
            services.AddTransient<INotificacaoService, NotificacaoService>();
            services.AddTransient<INotificacaoStatsService, NotificacaoStatsService>();

            services.AddScoped<
                IDomainEventHandler<AgendamentoCanceladoEvent>,
                AgendamentoCanceladoNotificacaoHandler>();

            services.AddHostedService<OutboxProcessorWorker>();
            services.AddHostedService<RealtimeNotificationListener>();

            return services;
        }
    }
}