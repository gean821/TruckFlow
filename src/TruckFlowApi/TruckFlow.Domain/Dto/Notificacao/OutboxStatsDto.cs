namespace TruckFlow.Domain.Dto.Notificacao
{
    public sealed record OutboxStatsDto(
        int Pending,
        int FailedDefinitive,
        int ProcessedLastHour,
        double? AverageLagMsLastHour
    );
}