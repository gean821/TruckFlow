using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Application.Notificacoes.Handlers;
using TruckFlow.Domain.Events;
using TruckFlowApi.Infra.Database.Interceptors;
using TruckFlowApi.Infra.Notifications.Expo;
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
            services.AddTransient<INotificacaoEntregaRepositorio, NotificacaoEntregaRepositorio>();
            services.AddTransient<IDispositivoUsuarioRepositorio, DispositivoUsuarioRepositorio>();
            services.AddTransient<INotificacaoService, NotificacaoService>();
            services.AddTransient<INotificacaoSendService, NotificacaoSendService>();
            services.AddTransient<INotificacaoStatsService, NotificacaoStatsService>();
            services.AddTransient<IDispositivoUsuarioService, DispositivoUsuarioService>();

            services.AddHttpClient<IExpoPushClient, ExpoPushClient>(c =>
            {
                c.BaseAddress = new Uri("https://exp.host");
                c.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                c.Timeout = TimeSpan.FromSeconds(30);
            });

            services.AddScoped<
                IDomainEventHandler<AgendamentoCanceladoEvent>,
                AgendamentoCanceladoNotificacaoHandler>();

            services.AddScoped<
                IDomainEventHandler<AgendamentoEvent.MotoristaChegouEvent>,
                MotoristaChegouNotificacaoHandler>();

            services.AddScoped<
                IDomainEventHandler<AgendamentoEvent.AgendamentoReagendadoEvent>,
                AgendamentoReagendadoNotificacaoHandler>();

            services.AddHostedService<OutboxProcessorWorker>();
            services.AddHostedService<RealtimeNotificationListener>();
            services.AddHostedService<PushDispatcherWorker>();
            services.AddHostedService<ReceiptPollerWorker>();

            return services;
        }
    }
}