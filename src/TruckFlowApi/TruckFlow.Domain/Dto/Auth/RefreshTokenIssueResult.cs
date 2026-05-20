namespace TruckFlow.Domain.Dto.Auth
{
    public sealed record RefreshTokenIssueResult(
        string RawToken,
        DateTime ExpiresAt,
        Guid FamilyId,
        Guid TokenId
    );
}
