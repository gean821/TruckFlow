namespace TruckFlow.Domain.Dto.Notificacao
{
    public sealed record NotificacaoEventDto(
        Guid EmpresaId,
        Guid UsuarioId,
        Guid NotificacaoId,
        int Tipo,
        int Prioridade,
        string Titulo,
        string Corpo,
        DateTime CriadaEm
    );
}
