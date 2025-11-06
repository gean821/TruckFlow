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
    public class ItemPlanejamentoRepositorio : IItemPlanejamentoRepositorio
    {

        private readonly AppDbContext _db;
        public ItemPlanejamentoRepositorio(AppDbContext db) =>
            _db = db;
        public async Task<ItemPlanejamento> CreateItem
            (
                ItemPlanejamento itemPlanejamento,
                CancellationToken token = default
            )
        {
            await _db.ItensPlanejamento.AddAsync(itemPlanejamento, token);
            await SaveChangesAsync(token);
            return itemPlanejamento;
        }
        public async Task<List<ItemPlanejamento>> GetAll(CancellationToken token = default)
        {
            return await _db.ItensPlanejamento
                .Include(x => x.Produto)
                .Include(x => x.PlanejamentoRecebimento)
                    .ThenInclude(y=> y!.Fornecedor)
                .ToListAsync(token);
        }

        public async Task<ItemPlanejamento> GetById(Guid id, CancellationToken token = default)
        {
            var item = await _db.ItensPlanejamento
                  .Include(x => x.Produto)
                   .Include(x => x.PlanejamentoRecebimento)
                    .ThenInclude(y=> y!.Fornecedor)
                   .FirstOrDefaultAsync(x => x.Id == id, token);

            return item!;
        }

        public async Task<ItemPlanejamento> UpdateItem
            (
                Guid id,
                ItemPlanejamento item,
                CancellationToken token = default
           )
        {
            _db.ItensPlanejamento.Update(item);
            await SaveChangesAsync(token);
            return item;
        }
        public async Task DeleteItem(Guid id, CancellationToken token = default)
        {
            var item = await GetById(id, token);
            _db.Remove(item);
            await SaveChangesAsync(token);
        }
        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }
    }
}
