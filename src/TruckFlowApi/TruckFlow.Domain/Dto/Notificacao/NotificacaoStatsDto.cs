namespace TruckFlow.Domain.Dto.Notificacao
{
    public sealed record NotificacaoStatsDto(
        int SseActiveConnections,
        int SseActiveUsers,
        OutboxStatsDto Outbox
    );
}