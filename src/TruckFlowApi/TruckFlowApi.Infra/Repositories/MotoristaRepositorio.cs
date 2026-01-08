using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class MotoristaRepositorio : IMotoristaRepositorio
    {
        private readonly AppDbContext _db;

        public MotoristaRepositorio(AppDbContext db) => _db = db;
        
        public async Task<Motorista?> GetByUsuarioId(Guid usuarioId, CancellationToken token = default)
        {
            return await _db.Motorista
                .Include(x => x.Veiculos)
                .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId);
        }

        public async Task<List<Veiculo>> GetMeusVeiculos(Guid usuarioId, CancellationToken token = default)
        {
            return await _db.Veiculo
               .Where(x => x.Motorista.UsuarioId == usuarioId)
               .ToListAsync(token);
        }


        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
