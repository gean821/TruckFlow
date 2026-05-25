using System.Text.Json;
using Npgsql;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Database.Interceptors;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class RealtimeNotificationListener : BackgroundService
    {
        private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

        private readonly IConfiguration _config;
        private readonly INotificationConnectionManager _manager;
        private readonly ILogger<RealtimeNotificationListener> _logger;

        public RealtimeNotificationListener
            (
                IConfiguration config,
                INotificationConnectionManager manager,
                ILogger<RealtimeNotificationListener> logger
            )
        {
            _config = config;
            _manager = manager;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connString = _config.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionString DefaultConnection ausente.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var conn = new NpgsqlConnection(connString);
                    await conn.OpenAsync(stoppingToken);

                    conn.Notification += OnNotification;

                    await using (var cmd = new NpgsqlCommand($"LISTEN {RealtimeNotificationInterceptor.ChannelName};", conn))
                    {
                        await cmd.ExecuteNonQueryAsync(stoppingToken);
                    }

                    _logger.LogInformation(
                        "RealtimeNotificationListener: LISTEN {Channel} ativo.",
                        RealtimeNotificationInterceptor.ChannelName);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        await conn.WaitAsync(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "RealtimeNotificationListener desconectou; reconectando em {Delay}.",
                        ReconnectDelay);

                    try
                    {
                        await Task.Delay(ReconnectDelay, stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation("RealtimeNotificationListener finalizado.");
        }

        private void OnNotification
            (
                object? sender,
                NpgsqlNotificationEventArgs args
            )
        {
            try
            {
                var evt = JsonSerializer.Deserialize<NotificacaoEventDto>(
                    args.Payload,
                    OutboxEventSerializer.Options);

                if (evt is null)
                {
                    return;
                }

                _manager.PublishToUser(evt.UsuarioId, evt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falha processando notificação LISTEN: {Payload}",
                    args.Payload);
            }
        }
    }
}