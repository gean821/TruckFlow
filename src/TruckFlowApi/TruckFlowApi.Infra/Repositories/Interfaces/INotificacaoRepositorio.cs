using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotificacaoRepositorio
    {
        Task<List<Notificacao>> ListByUserAsync(
            Guid userId,
            int skip,
            int take,
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