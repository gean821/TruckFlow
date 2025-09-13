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

    public class ProdutoRepositorio : IProdutoRepositorio
    {
        private readonly AppDbContext _db;

        public ProdutoRepositorio(AppDbContext db) => _db = db;

        public async Task<Produto> CreateProduto(Produto produto, CancellationToken cancellationToken = default)
        {
            await _db.Produto.AddAsync(produto, cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return produto;
        }


        public async Task<List<Produto>> GetAll(CancellationToken cancellationToken = default) =>
            await _db.Produto
            .Include(x => x.LocalDescarga)
            .ToListAsync(cancellationToken);



        public async Task<Produto> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var produto = await _db.Produto.FindAsync(id, cancellationToken);
            return produto!;
        }
        

        public async Task<Produto> UpdateProduto(Guid id, Produto produto, CancellationToken cancellationToken = default)
        {
            var produtoBuscado = await GetById(id, cancellationToken);

            produtoBuscado.LocalDescarga = produto.LocalDescarga;
            produtoBuscado.Id = produto.Id;
            
            _db.Produto.Update(produto);

            await SaveChangesAsync(cancellationToken);
            return produtoBuscado;
        }
        public async Task DeleteProduto(Guid id, CancellationToken cancellationToken = default) 
        {
            var produtoDeletado = await _db.Produto.FindAsync(id, cancellationToken);
            _db.Remove(produtoDeletado!);
            await SaveChangesAsync(cancellationToken);
        }
        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
