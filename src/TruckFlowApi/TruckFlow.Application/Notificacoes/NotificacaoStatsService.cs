using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.Notificacoes
{
    public sealed class NotificacaoStatsService : INotificacaoStatsService
    {
        private readonly IOutboxEventRepositorio _outboxRepo;
        private readonly INotificationConnectionManager _connections;

        public NotificacaoStatsService(
            IOutboxEventRepositorio outboxRepo,
            INotificationConnectionManager connections)
        {
            _outboxRepo = outboxRepo;
            _connections = connections;
        }

        public async Task<NotificacaoStatsDto> GetAsync(CancellationToken token = default)
        {
            var outbox = await _outboxRepo.GetStatsAsync(token);

            return new NotificacaoStatsDto(
                SseActiveConnections: _connections.ActiveConnectionCount(),
                SseActiveUsers: _connections.ActiveUserCount(),
                Outbox: outbox);
        }
    }
}