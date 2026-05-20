using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class RefreshTokenRepositorio : IRefreshTokenRepositorio
    {
        private readonly AppDbContext _db;

        public RefreshTokenRepositorio(AppDbContext db) => _db = db;

        public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken token = default) =>
            _db.RefreshToken.FirstOrDefaultAsync(x => x.TokenHash == tokenHash, token);

        public Task AddAsync(RefreshToken refreshToken, CancellationToken token = default)
        {
            _db.RefreshToken.Add(refreshToken);
            return Task.CompletedTask;
        }

        public Task<int> RevokeFamilyAsync(
            Guid familyId,
            DateTime nowUtc,
            string reason,
            CancellationToken token = default) =>
            _db.RefreshToken
                .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedAt, nowUtc)
                    .SetProperty(x => x.RevokedReason, reason), token);

        public Task<int> RevokeAllForUserAsync(
            Guid userId,
            DateTime nowUtc,
            string reason,
            CancellationToken token = default) =>
            _db.RefreshToken
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedAt, nowUtc)
                    .SetProperty(x => x.RevokedReason, reason), token);

        public Task SaveChangesAsync(CancellationToken token = default) =>
            _db.SaveChangesAsync(token);
    }
}
