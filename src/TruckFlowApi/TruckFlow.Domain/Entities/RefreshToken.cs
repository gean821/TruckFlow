namespace TruckFlow.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }
        public Usuario? Usuario { get; set; }

        public required string TokenHash { get; set; }

        public Guid FamilyId { get; set; }

        public Guid? EmpresaId { get; set; }

        public string? DeviceInfo { get; set; }
        public string? IpAddress { get; set; }

        public required DateTime CreatedAt { get; set; }
        public required DateTime ExpiresAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? RevokedReason { get; set; }
        public Guid? ReplacedByTokenId { get; set; }

        public bool IsActive(DateTime nowUtc) =>
            RevokedAt is null && nowUtc < ExpiresAt;
    }
}
