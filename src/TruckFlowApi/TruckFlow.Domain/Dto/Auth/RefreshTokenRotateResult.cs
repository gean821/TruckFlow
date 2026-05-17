namespace TruckFlow.Domain.Dto.Auth
{
    public enum RefreshTokenRotateFailure
    {
        NotFound,
        Expired,
        Revoked,
        ReuseDetected
    }

    public sealed record RefreshTokenRotateResult
    {
        public bool Success { get; private init; }
        public string? NewRawToken { get; private init; }
        public DateTime? NewExpiresAt { get; private init; }
        public Guid? NewTokenId { get; private init; }
        public Guid? UserId { get; private init; }
        public Guid? FamilyId { get; private init; }
        public RefreshTokenRotateFailure? FailureReason { get; private init; }

        public static RefreshTokenRotateResult Ok(
            string rawToken,
            DateTime expiresAt,
            Guid newTokenId,
            Guid userId,
            Guid familyId) =>
            new()
            {
                Success = true,
                NewRawToken = rawToken,
                NewExpiresAt = expiresAt,
                NewTokenId = newTokenId,
                UserId = userId,
                FamilyId = familyId
            };

        public static RefreshTokenRotateResult Fail(RefreshTokenRotateFailure reason) =>
            new() { Success = false, FailureReason = reason };
    }
}
