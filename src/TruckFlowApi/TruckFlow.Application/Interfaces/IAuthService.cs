using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AccessTokenResult> GenerateTokenAsync(Usuario usuario, CancellationToken token = default);

        Task<RefreshAccessResult> RefreshAccessTokenAsync(
            string rawRefreshToken,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken token = default);
    }
}