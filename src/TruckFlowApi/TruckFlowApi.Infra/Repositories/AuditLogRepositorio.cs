using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class AuditLogRepositorio : IAuditLogRepositorio
    {
        private readonly AppDbContext _db;

        public AuditLogRepositorio(AppDbContext db) => _db = db;

        public async Task<PagedResponse<AuditLog>> GetPagedAsync(
            AuditLogListQueryDto query,
            CancellationToken token = default)
        {
            var dbQuery = _db.AuditLog
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.EntityName))
            {
                dbQuery = dbQuery.Where(x => x.EntityName == query.EntityName);
            }

            if (!string.IsNullOrWhiteSpace(query.EntityId))
            {
                dbQuery = dbQuery.Where(x => x.EntityId == query.EntityId);
            }

            if (query.Action.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.Action == query.Action.Value);
            }

            if (query.UserId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.UserId == query.UserId.Value);
            }

            if (query.DataInicio.HasValue)
            {
                var inicio = DateTime.SpecifyKind(query.DataInicio.Value, DateTimeKind.Utc);
                dbQuery = dbQuery.Where(x => x.Timestamp >= inicio);
            }

            if (query.DataFim.HasValue)
            {
                var fim = DateTime.SpecifyKind(query.DataFim.Value, DateTimeKind.Utc);
                dbQuery = dbQuery.Where(x => x.Timestamp <= fim);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                dbQuery = dbQuery.Where(x =>
                    x.EntityName.Contains(search) ||
                    x.EntityId.Contains(search));
            }

            var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
            var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

            var totalCount = await dbQuery.CountAsync(token);

            var items = await dbQuery
                .OrderByDescending(x => x.Timestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(token);

            return new PagedResponse<AuditLog>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<IReadOnlyDictionary<Guid, string?>> GetUserNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken token = default)
        {
            if (userIds.Count == 0)
            {
                return new Dictionary<Guid, string?>();
            }

            var pares = await _db.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UserName })
                .ToListAsync(token);

            return pares.ToDictionary(x => x.Id, x => x.UserName);
        }
    }
}
