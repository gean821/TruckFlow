using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class AuditLog : IEmpresaScoped
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public Guid? UserId { get; set; }

        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;

        public AuditAction Action { get; set; }
        public string? ChangesJson { get; set; }

        public DateTime Timestamp { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
