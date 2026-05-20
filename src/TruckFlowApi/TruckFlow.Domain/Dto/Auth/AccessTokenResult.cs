namespace TruckFlow.Domain.Dto.Auth
{
    public sealed record AccessTokenResult(string Token, DateTime ExpiresAt);
}
