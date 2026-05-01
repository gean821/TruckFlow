using System.Text.Json;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepositorio _repo;

        public AuditLogService(IAuditLogRepositorio repo)
        {
            _repo = repo;
        }

        public async Task<PagedResponse<AuditLogResponseDto>> GetPaged(
            AuditLogListQueryDto query,
            CancellationToken token = default)
        {
            var paged = await _repo.GetPagedAsync(query, token);

            var userIds = paged.Items
                .Where(x => x.UserId.HasValue)
                .Select(x => x.UserId!.Value)
                .Distinct()
                .ToList();

            var userNames = await _repo.GetUserNamesAsync(userIds, token);

            return new PagedResponse<AuditLogResponseDto>
            {
                Items = paged.Items.Select(x => MapToResponse(x, userNames)).ToList(),
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalCount = paged.TotalCount,
                TotalPages = paged.TotalPages
            };
        }

        private static AuditLogResponseDto MapToResponse(
            AuditLog entry,
            IReadOnlyDictionary<Guid, string?> userNames)
        {
            return new AuditLogResponseDto
            {
                Id = entry.Id,
                EntityName = entry.EntityName,
                EntityId = entry.EntityId,
                Action = entry.Action.ToString(),
                UserId = entry.UserId,
                UserName = entry.UserId.HasValue && userNames.TryGetValue(entry.UserId.Value, out var name)
                    ? name
                    : null,
                Timestamp = entry.Timestamp,
                Changes = ParseChanges(entry.ChangesJson),
                IpAddress = entry.IpAddress,
                UserAgent = entry.UserAgent
            };
        }

        private static object? ParseChanges(string? changesJson)
        {
            if (string.IsNullOrWhiteSpace(changesJson))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<JsonElement>(changesJson);
            }
            catch
            {
                return changesJson;
            }
        }
    }
}
