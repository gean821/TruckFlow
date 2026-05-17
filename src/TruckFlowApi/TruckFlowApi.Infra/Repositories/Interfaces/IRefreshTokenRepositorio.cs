using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRefreshTokenRepositorio
    {
        Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken token = default);

        Task AddAsync(RefreshToken refreshToken, CancellationToken token = default);

        Task<int> RevokeFamilyAsync(
            Guid familyId,
            DateTime nowUtc,
            string reason,
            CancellationToken token = default);

        Task<int> RevokeAllForUserAsync(
            Guid userId,
            DateTime nowUtc,
            string reason,
            CancellationToken token = default);

        Task SaveChangesAsync(CancellationToken token = default);
    }
}
