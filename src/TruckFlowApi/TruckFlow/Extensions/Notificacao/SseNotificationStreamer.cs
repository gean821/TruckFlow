using System.Text.Json;
using System.Threading.Channels;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class SseNotificationStreamer
    {
        private static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(25);

        private readonly INotificationConnectionManager _manager;
        private readonly ILogger<SseNotificationStreamer> _logger;

        public SseNotificationStreamer(
            INotificationConnectionManager manager,
            ILogger<SseNotificationStreamer> logger)
        {
            _manager = manager;
            _logger = logger;
        }

        public async Task StreamAsync(
            HttpResponse response,
            Guid usuarioId,
            CancellationToken cancellationToken)
        {
            ApplySseHeaders(response);
            await response.Body.FlushAsync(cancellationToken);

            var channel = _manager.Register(usuarioId);

            _logger.LogInformation("SSE conectado: user {UserId}.", usuarioId);

            try
            {
                await PumpAsync(response, channel.Reader, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Cliente desconectou ou shutdown
            }
            finally
            {
                _manager.Unregister(usuarioId, channel);
                _logger.LogInformation("SSE desconectado: user {UserId}.", usuarioId);
            }
        }

        private static void ApplySseHeaders(HttpResponse response)
        {
            response.ContentType = "text/event-stream";
            response.Headers["Cache-Control"] = "no-cache";
            response.Headers["X-Accel-Buffering"] = "no";
            response.Headers["Connection"] = "keep-alive";
        }

        private static async Task PumpAsync(
            HttpResponse response,
            ChannelReader<NotificacaoEventDto> reader,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var readTask = reader.WaitToReadAsync(cancellationToken).AsTask();
                var keepAliveTask = Task.Delay(KeepAliveInterval, cancellationToken);

                var completed = await Task.WhenAny(readTask, keepAliveTask);

                if (completed == keepAliveTask)
                {
                    await response.WriteAsync(": keepalive\n\n", cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                    continue;
                }

                var hasData = await readTask;
                if (!hasData)
                {
                    break;
                }

                while (reader.TryRead(out var evt))
                {
                    await WriteEventAsync(response, evt, cancellationToken);
                }
            }
        }

        private static async Task WriteEventAsync(
            HttpResponse response,
            NotificacaoEventDto evt,
            CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(evt, OutboxEventSerializer.Options);
            await response.WriteAsync($"id: {evt.NotificacaoId}\n", cancellationToken);
            await response.WriteAsync("event: notification\n", cancellationToken);
            await response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
    }
}