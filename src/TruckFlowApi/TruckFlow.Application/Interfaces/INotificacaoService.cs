using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Dto.Shared;

namespace TruckFlow.Application.Interfaces
{
    public interface INotificacaoService
    {
        Task<PagedResponse<NotificacaoListItemDto>> ListarMinhasAsync(
            NotificacaoListQueryDto query,
            CancellationToken ct);

        Task<int> ContarNaoLidasMinhasAsync(CancellationToken ct);

        Task<bool> MarcarComoLidaAsync(Guid notificacaoId, CancellationToken ct);
    }
}