using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Dto.Shared;

namespace TruckFlow.Application.Interfaces
{
    public interface IAuditLogService
    {
        Task<PagedResponse<AuditLogResponseDto>> GetPaged(
            AuditLogListQueryDto query,
            CancellationToken token = default);
    }
}
