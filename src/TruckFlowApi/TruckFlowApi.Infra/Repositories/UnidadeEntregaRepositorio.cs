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
    public class UnidadeEntregaRepositorio(AppDbContext db) : IUnidadeEntregaRepositorio
    {
        private readonly AppDbContext _db = db;

        public async Task<UnidadeEntrega> CreateUnidadeEntrega(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            await _db.UnidadeEntrega.AddAsync(unidade, token);
            return unidade;
        }
       
        public async Task<List<UnidadeEntrega>> GetAll(
            CancellationToken token = default)
        {
            return await _db.UnidadeEntrega
            .Include(x=> x.LocaisDeDescarga)
            .ToListAsync(token);
        }

        public async Task<UnidadeEntrega?> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            return await _db.UnidadeEntrega
                .Include(x => x.LocaisDeDescarga)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<UnidadeEntrega> Update(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            _db.UnidadeEntrega.Update(unidade);
            await SaveChangesAsync(token);
            return unidade;
        }

        public async Task Delete(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            _db.Remove(unidade);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
