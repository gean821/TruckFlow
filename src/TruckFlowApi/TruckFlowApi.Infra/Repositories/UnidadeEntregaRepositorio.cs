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
    public class UnidadeEntregaRepositorio : IUnidadeEntregaRepositorio
    {
        private readonly AppDbContext _db;

        public UnidadeEntregaRepositorio(AppDbContext db) => _db = db;

        public async Task<UnidadeEntrega> CreateUnidadeEntrega(
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            await _db.UnidadeEntrega.AddAsync(unidade, token);
            return unidade;
        }
        
        public async Task<List<UnidadeEntrega>> GetAll(CancellationToken token = default)
        {
            return await _db.UnidadeEntrega
            .Include(x => x.Agendamento)
            .ToListAsync();
        }

        public async Task<UnidadeEntrega> GetById(
            Guid id,
            CancellationToken token = default)
        {
            var unidadeEntrega = await _db.UnidadeEntrega.FindAsync(id, token);
            return unidadeEntrega!;
        }

        public async Task<UnidadeEntrega> Update(
            Guid id,
            UnidadeEntrega unidade,
            CancellationToken token = default)
        {
            var unidadeEncontrada = await GetById(id, token);

            unidadeEncontrada.Id = unidade.Id;
            unidadeEncontrada.Localizacao = unidade.Localizacao;
            unidadeEncontrada.Agendamento = unidade.Agendamento;

            await SaveChangesAsync(token);
            return unidadeEncontrada;
        }

        public async Task Delete(
            Guid id,
            CancellationToken token = default)
        {
            var unidadeEncontrada = await GetById(id,token);
            _db.Remove(unidadeEncontrada);
            await SaveChangesAsync(token);
        }

        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
