using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotificacaoRepositorio
    {
        Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedAsync(
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token = default);

        Task<int> CountUnreadByUserAsync(
            Guid userId,
            CancellationToken token = default);

        Task<Notificacao?> GetByIdForUserAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default);

        Task<Notificacao> UpdateAsync(
            Notificacao notificacao,
            CancellationToken token = default);
    }
}