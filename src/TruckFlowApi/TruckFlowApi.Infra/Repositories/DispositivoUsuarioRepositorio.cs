using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class DispositivoUsuarioRepositorio : IDispositivoUsuarioRepositorio
    {
        private readonly AppDbContext _db;

        public DispositivoUsuarioRepositorio(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DispositivoUsuario?> GetByTokenAsync(
            string expoPushToken,
            CancellationToken token = default)
        {
            return await _db.DispositivoUsuario
                .FirstOrDefaultAsync(d => d.ExpoPushToken == expoPushToken, token);
        }

        public async Task<List<DispositivoUsuario>> GetAtivosByUsuarioAsync(
            Guid usuarioId,
            CancellationToken token = default)
        {
            return await _db.DispositivoUsuario
                .AsNoTracking()
                .Where(d => d.UsuarioId == usuarioId && d.Ativo)
                .ToListAsync(token);
        }

        public async Task<DispositivoUsuario> AddAsync(
            DispositivoUsuario dispositivo,
            CancellationToken token = default)
        {
            await _db.DispositivoUsuario.AddAsync(dispositivo, token);
            await _db.SaveChangesAsync(token);
            return dispositivo;
        }

        public async Task<DispositivoUsuario> UpdateAsync(
            DispositivoUsuario dispositivo,
            CancellationToken token = default)
        {
            _db.DispositivoUsuario.Update(dispositivo);
            await _db.SaveChangesAsync(token);
            return dispositivo;
        }
    }
}
