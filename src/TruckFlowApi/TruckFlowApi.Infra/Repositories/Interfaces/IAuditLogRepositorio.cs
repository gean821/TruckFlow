using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IAuditLogRepositorio
    {
        Task<PagedResponse<AuditLog>> GetPagedAsync(
            AuditLogListQueryDto query,
            CancellationToken token = default);

        Task<IReadOnlyDictionary<Guid, string?>> GetUserNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken token = default);
    }
}
