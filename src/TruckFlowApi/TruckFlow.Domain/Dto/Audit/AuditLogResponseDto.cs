namespace TruckFlow.Domain.Dto.Audit
{
    public class AuditLogResponseDto
    {
        public Guid Id { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;

        public Guid? UserId { get; set; }
        public string? UserName { get; set; }

        public DateTime Timestamp { get; set; }
        public object? Changes { get; set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
