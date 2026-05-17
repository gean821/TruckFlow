using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlowApi.Infra.Database.Interceptors
{
    public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash",
            "SecurityStamp",
            "ConcurrencyStamp",
            "AccessFailedCount",
            "LockoutEnd",
            "LockoutEnabled",
            "TwoFactorEnabled",
            "PhoneNumberConfirmed",
            "EmailConfirmed"
        };

        private readonly ICurrentUserService _currentUser;
        private readonly IEmpresaContext _empresaContext;
        private readonly IHttpContextAccessor _http;

        public AuditSaveChangesInterceptor(
            ICurrentUserService currentUser,
            IEmpresaContext empresaContext,
            IHttpContextAccessor http)
        {
            _currentUser = currentUser;
            _empresaContext = empresaContext;
            _http = http;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            var ctx = eventData.Context;
            if (ctx is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            var userId = _currentUser.UserIdOrNull;
            var empresaId = _empresaContext.EmpresaIdOrNull;
            var ipAddress = _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = _http.HttpContext?.Request?.Headers["User-Agent"].ToString();
            var now = DateTime.UtcNow;

            var entries = ctx.ChangeTracker
                .Entries<EntidadeBase>()
                .Where(e =>
                    e.State == EntityState.Added ||
                    e.State == EntityState.Modified ||
                    e.State == EntityState.Deleted)
                .ToList();

            var auditLogs = new List<AuditLog>();

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added && entry.Entity.CreatedBy is null && userId.HasValue)
                {
                    entry.Entity.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified && userId.HasValue)
                {
                    entry.Entity.UpdatedBy = userId;
                }

                if (empresaId is null)
                {
                    continue;
                }

                var action = entry.State switch
                {
                    EntityState.Added => AuditAction.Create,
                    EntityState.Modified => AuditAction.Update,
                    EntityState.Deleted => AuditAction.Delete,
                    _ => AuditAction.Update
                };

                auditLogs.Add(new AuditLog
                {
                    EmpresaId = empresaId.Value,
                    UserId = userId,
                    EntityName = entry.Entity.GetType().Name,
                    EntityId = entry.Entity.Id.ToString(),
                    Action = action,
                    ChangesJson = BuildChanges(entry),
                    Timestamp = now,
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                });
            }

            if (auditLogs.Count > 0)
            {
                ctx.AddRange(auditLogs);
            }

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private static string? BuildChanges(EntityEntry entry)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                var name = prop.Metadata.Name;
                if (SensitivePropertyNames.Contains(name))
                {
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        if (prop.CurrentValue is not null)
                        {
                            dict[name] = prop.CurrentValue;
                        }
                        break;

                    case EntityState.Modified:
                        if (prop.IsModified && !Equals(prop.OriginalValue, prop.CurrentValue))
                        {
                            dict[name] = new
                            {
                                from = prop.OriginalValue,
                                to = prop.CurrentValue
                            };
                        }
                        break;

                    case EntityState.Deleted:
                        if (prop.OriginalValue is not null)
                        {
                            dict[name] = prop.OriginalValue;
                        }
                        break;
                }
            }

            return dict.Count == 0 ? null : JsonSerializer.Serialize(dict);
        }
    }
}
