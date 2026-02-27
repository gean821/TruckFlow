using Microsoft.AspNetCore.Http;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public sealed class EmpresaContext(
        IHttpContextAccessor httpContextAccessor): IEmpresaContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        public Guid EmpresaId
        {
            get
            {
                var claim = _httpContextAccessor
                    .HttpContext?
                    .User?
                    .FindFirst("EmpresaId")?
                    .Value;

                return Guid.TryParse(claim, out var empresaId)
                    ? empresaId
                    : Guid.Empty;
            }
        }
    }
}