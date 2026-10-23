using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;

namespace TruckFlow.Test.Integration.Fakes;

internal sealed class FakeRefreshTokenService : IRefreshTokenService
{
    public Task<RefreshTokenIssueResult> IssueAsync(
        Guid userId,
        Guid? empresaId,
        string? deviceInfo,
        string? ipAddress,
        Guid? familyId = null,
        CancellationToken ct = default
    ) =>
        Task.FromResult(
            new RefreshTokenIssueResult(
                "fake-refresh-token",
                DateTime.UtcNow.AddDays(30),
                familyId ?? Guid.NewGuid(),
                Guid.NewGuid()
            )
        );

    public Task<RefreshTokenRotateResult> RotateAsync(
        string rawToken,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken ct = default
    ) => throw new NotSupportedException("Não utilizado nestes testes.");

    public Task<bool> RevokeAsync(string rawToken, string reason, CancellationToken ct = default) =>
        throw new NotSupportedException("Não utilizado nestes testes.");

    public Task<int> RevokeFamilyAsync(
        Guid familyId,
        string reason,
        CancellationToken ct = default
    ) => throw new NotSupportedException("Não utilizado nestes testes.");

    public Task<int> RevokeAllForUserAsync(
        Guid userId,
        string reason,
        CancellationToken ct = default
    ) => throw new NotSupportedException("Não utilizado nestes testes.");
}
