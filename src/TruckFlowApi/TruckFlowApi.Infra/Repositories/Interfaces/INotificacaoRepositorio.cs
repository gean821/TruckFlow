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

        Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedAcrossTenantsAsync(
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token = default);

        Task<int> CountUnreadByUserAsync(
            Guid userId,
            CancellationToken token = default);

        Task<int> CountUnreadByUserAcrossTenantsAsync(
            Guid userId,
            CancellationToken token = default);

        Task<Notificacao?> GetByIdForUserAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default);

        Task<Notificacao?> GetByIdForUserAcrossTenantsAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default);

        Task<List<Notificacao>> ListByUserAndAgendamentoAsync(
            Guid userId,
            Guid agendamentoId,
            CancellationToken token = default);

        Task<List<Notificacao>> ListByUserAndAgendamentoAcrossTenantsAsync(
            Guid userId,
            Guid agendamentoId,
            CancellationToken token = default);

        Task<Notificacao> UpdateAsync(
            Notificacao notificacao,
            CancellationToken token = default);

        Task<Notificacao> AddAsync(
            Notificacao notificacao,
            CancellationToken token = default);

        Task AddRangeAsync(
            IEnumerable<Notificacao> notificacoes,
            CancellationToken token = default);
    }
}