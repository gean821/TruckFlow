using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly AppDbContext _db;

        public UsuarioRepositorio(AppDbContext db) => _db = db;

        public async Task<Usuario?> GetForTokenRefreshAsync(
            Guid userId,
            CancellationToken token = default) =>
            await _db.Users
                .Include(u => u.Motorista)
                .FirstOrDefaultAsync(u => u.Id == userId 
                    && u.DeletedAt == null, token);
    }
}