namespace TruckFlow.Domain.Dto.Auth
{
    public sealed record RefreshAccessResult
    {
        public bool Success { get; private init; }
        public string? AccessToken { get; private init; }
        public DateTime? AccessExpiresAt { get; private init; }
        public string? NewRefreshToken { get; private init; }
        public DateTime? NewRefreshExpiresAt { get; private init; }

        public static RefreshAccessResult Ok(
            string accessToken,
            DateTime accessExpiresAt,
            string newRefreshToken,
            DateTime newRefreshExpiresAt) =>
            new()
            {
                Success = true,
                AccessToken = accessToken,
                AccessExpiresAt = accessExpiresAt,
                NewRefreshToken = newRefreshToken,
                NewRefreshExpiresAt = newRefreshExpiresAt
            };

        public static RefreshAccessResult Fail() => new() { Success = false };
    }
}