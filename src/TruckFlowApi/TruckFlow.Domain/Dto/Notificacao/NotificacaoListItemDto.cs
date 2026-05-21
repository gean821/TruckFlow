namespace TruckFlow.Domain.Dto.Notificacao
{
    public sealed record NotificacaoListItemDto(
        Guid Id,
        int Tipo,
        int Prioridade,
        string Titulo,
        string Corpo,
        DateTime CriadaEm,
        DateTime? LidaEm,
        string PayloadJson
    );
}
