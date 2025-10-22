using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
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
            CancellationToken token = default)
        {
            await _db.LocalDescarga.AddAsync(local,token);
            return local;
        }

        public async Task<List<LocalDescarga>> GetAll(CancellationToken token = default)
        {
                 return await _db.LocalDescarga
                .Include(x=> x.Produtos)
                .ToListAsync(token);
        }

        public async Task<LocalDescarga> GetById(
            Guid id,
            CancellationToken token = default)
        {
            var localDescarga = await _db.LocalDescarga.FindAsync(id, token);
            return localDescarga!;
        }
        public async Task<LocalDescarga> Update(
            Guid id,
            LocalDescarga local,
            CancellationToken token = default)
        {
            var localEncontrado = await GetById(id, token);
            
            localEncontrado.Id = local.Id;
            localEncontrado.Nome = local.Nome;
            localEncontrado.Produtos = local.Produtos;
            
            await SaveChangesAsync(token);
            return localEncontrado;
        }
        public async Task Delete(
            Guid id,
            CancellationToken token = default)
        {
            var localEncontrado = await GetById(id, token);
            
            _db.Remove(localEncontrado);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
