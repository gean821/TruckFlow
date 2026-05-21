using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Application.Interfaces
{
    public interface INotificacaoService
    {
        Task<IReadOnlyList<NotificacaoListItemDto>> ListarMinhasAsync(int skip, int take, CancellationToken ct);
        Task<int> ContarNaoLidasMinhasAsync(CancellationToken ct);
        Task<bool> MarcarComoLidaAsync(Guid notificacaoId, CancellationToken ct);
    }
}