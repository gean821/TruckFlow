using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.LocalDescarga;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class LocalDescargaRepositorio : ILocalDescargaRepositorio
    {
        private readonly AppDbContext _db;

        public LocalDescargaRepositorio(AppDbContext db) => _db = db;
       
        public async Task<LocalDescarga> CreateLocalDescarga(
            LocalDescarga local,
            CancellationToken token = default
            )
        {
            await _db.LocalDescarga.AddAsync(local,token);
            return local;
        }

        public async Task<List<LocalDescarga>> GetAll(LocalDescargaListQueryDto query, CancellationToken token = default)
        {
            var dbQuery = _db.LocalDescarga
                .AsNoTracking()
                .Include(x => x.Produtos)
                .Include(x => x.UnidadeEntrega)
                .AsQueryable();

            if (query.Ativa.HasValue)
            {
                dbQuery = query.Ativa.Value
                    ? dbQuery.Where(x => x.Ativa == true)
                    : dbQuery.Where(x => x.Ativa != true);
            }

            if (query.UnidadeEntregaId.HasValue)
            {
                dbQuery = dbQuery.Where(x => x.UnidadeEntregaId == query.UnidadeEntregaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.Trim();
                dbQuery = dbQuery.Where(x =>
                    x.Nome.Contains(search) ||
                    (x.UnidadeEntrega != null && x.UnidadeEntrega.Nome.Contains(search)));
            }

            return await dbQuery
                .OrderBy(x => x.Nome)
                .ToListAsync(token);
        }

        public async Task<LocalDescarga?> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            return await _db.LocalDescarga
                 .Include(x => x.Produtos)
                 .Include(x => x.UnidadeEntrega)
                 .FirstOrDefaultAsync(x => x.Id == id, token);
        }
        public async Task<LocalDescarga> Update(
            LocalDescarga local,
            CancellationToken token = default)
        {
            _db.LocalDescarga.Update(local);
            await SaveChangesAsync(token);
            return local;
        }
        public async Task Delete(
            LocalDescarga local,
            CancellationToken token = default)
        {
            _db.Remove(local);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
