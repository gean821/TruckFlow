using Microsoft.AspNetCore.Http;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public sealed class EmpresaContext(
        IHttpContextAccessor httpContextAccessor) : IEmpresaContext
    {
        private static readonly AsyncLocal<Guid?> _tenantOverride = new();

        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        public Guid EmpresaId
        {
            get
            {
                var id = EmpresaIdOrNull;
                
                if (id is null)
                {
                    throw new UnauthorizedAccessException("Tenant não identificado.");
                }

                return id.Value;
            }
        }

        public Guid? EmpresaIdOrNull
        {
            get
            {
                if (_tenantOverride.Value is { } overrideId)
                {
                    return overrideId;
                }

                var claim = _httpContextAccessor
                    .HttpContext?
                    .User?
                    .FindFirst("EmpresaId")?
                    .Value;

                if (string.IsNullOrWhiteSpace(claim))
                {
                    return null;
                }

                if (!Guid.TryParse(claim, out var empresaId))
                {
                    throw new UnauthorizedAccessException("Tenant inválido.");
                }

                return empresaId;
            }
        }

        public IDisposable WithTenant(Guid empresaId)
        {
            var previous = _tenantOverride.Value;
            _tenantOverride.Value = empresaId;
            return new TenantScope(previous);
        }

        private sealed class TenantScope(Guid? previous) : IDisposable
        {
            public void Dispose() => _tenantOverride.Value = previous;
        }
    }
}
