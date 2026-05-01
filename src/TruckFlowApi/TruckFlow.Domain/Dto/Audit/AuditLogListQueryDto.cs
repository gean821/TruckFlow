using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Audit
{
    public class AuditLogListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? EntityName { get; set; }
        public string? EntityId { get; set; }
        public AuditAction? Action { get; set; }
        public Guid? UserId { get; set; }

        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public string? Search { get; set; }
    }
}
