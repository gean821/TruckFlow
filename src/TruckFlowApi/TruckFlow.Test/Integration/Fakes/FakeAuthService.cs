using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Test.Integration.Fakes;

internal sealed class FakeAuthService : IAuthService
{
    public Task<AccessTokenResult> GenerateTokenAsync(
        Usuario usuario,
        CancellationToken token = default
    ) =>
        Task.FromResult(new AccessTokenResult("fake-access-token", DateTime.UtcNow.AddMinutes(15)));

    public Task<RefreshAccessResult> RefreshAccessTokenAsync(
        string rawRefreshToken,
        string? deviceInfo,
        string? ipAddress,
        CancellationToken token = default
    ) => throw new NotSupportedException("Não utilizado nestes testes.");
}
