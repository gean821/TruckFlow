using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{

    public class ProdutoRepositorio(AppDbContext db) : IProdutoRepositorio
    {
        private readonly AppDbContext _db = db;

        public async Task<Produto> CreateProduto(
            Produto produto,
            CancellationToken cancellationToken = default
            )
        {
            await _db.Produto.AddAsync(produto, cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return produto;
        }

        public async Task<List<Produto>> GetAll(CancellationToken cancellationToken = default) =>
            await _db.Produto
            .Include(x=> x.LocalDescarga)
            .ToListAsync(cancellationToken);

        public async Task<Produto?> GetById(
            Guid id,
            CancellationToken cancellationToken = default
            )
        {
            return await _db.Produto
                .Include(x=> x.LocalDescarga)
                .FirstOrDefaultAsync(x=> x.Id == id, cancellationToken);
        }
        
        public async Task<Produto> UpdateProduto(
            Produto produto,
            CancellationToken cancellationToken = default
            )
        {
            _db.Produto.Update(produto);
            await SaveChangesAsync(cancellationToken);
            return produto;
        }
        public async Task DeleteProduto(
            Produto produto,
            CancellationToken cancellationToken = default
            ) 
        {
            _db.Remove(produto);
            await SaveChangesAsync(cancellationToken);
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<Produto>> GetByIdsAsync(IEnumerable<Guid> produtoIds, CancellationToken token)
        {
            return await _db.Produto
                .Where(p => produtoIds.Contains(p.Id))
                .Include(p => p.LocalDescarga)
                .ToListAsync(token);
        }
    }
}
