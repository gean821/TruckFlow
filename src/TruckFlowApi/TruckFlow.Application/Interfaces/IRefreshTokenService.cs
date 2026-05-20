using TruckFlow.Domain.Dto.Auth;

namespace TruckFlow.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshTokenIssueResult> IssueAsync(
            Guid userId,
            Guid? empresaId,
            string? deviceInfo,
            string? ipAddress,
            Guid? familyId = null,
            CancellationToken ct = default);

        Task<RefreshTokenRotateResult> RotateAsync(
            string rawToken,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken ct = default);

        Task<bool> RevokeAsync(
            string rawToken,
            string reason,
            CancellationToken ct = default);

        Task<int> RevokeFamilyAsync(
            Guid familyId,
            string reason,
            CancellationToken ct = default);

        Task<int> RevokeAllForUserAsync(
            Guid userId,
            string reason,
            CancellationToken ct = default);
    }
}
