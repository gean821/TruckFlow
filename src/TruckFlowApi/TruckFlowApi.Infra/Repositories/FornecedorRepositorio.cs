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
    public class FornecedorRepositorio(AppDbContext db) : IFornecedorRepositorio
    {
        private readonly AppDbContext _db = db;

        public async Task<Fornecedor> CreateFornecedor(
            Fornecedor fornecedor,
            CancellationToken token = default
            )
        {
            await _db.Fornecedor.AddAsync(fornecedor, token);
            return fornecedor;
        }

        public async Task<List<Fornecedor>> GetAll(CancellationToken token = default)
        {
            return await _db.Fornecedor
                .Include(x => x.Produtos)
                    .ThenInclude(x => x.LocalDescarga)
                .Include(x => x.NotaFiscal)
                .Include(x => x.Agendamentos)
                .ToListAsync(token);
        }

        public async Task<Fornecedor?> GetByCnpj(
            string cnpj,
            CancellationToken token = default
            )
        {
            var cnpjLimpo = new string(cnpj.Where(char.IsDigit).ToArray());

            return await _db.Fornecedor
                .Include(x=> x.Produtos)
                .Include(x=> x.Produtos)
                .Include(x=> x.Agendamentos)
                .FirstOrDefaultAsync(x => x.Cnpj == cnpjLimpo, token);
        }

        public async Task<Fornecedor?> GetById(
            Guid id,
            CancellationToken token = default
            )
        {
            return await _db.Fornecedor
                .Include(x => x.Agendamentos)
                .Include(x => x.Produtos)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }

        public async Task<Fornecedor> Update(
            Fornecedor fornecedor,
            CancellationToken token = default
            )
        {
            _db.Fornecedor.Update(fornecedor);
            await SaveChangesAsync(token);
            return fornecedor;
        }

        public async Task Delete(
            Fornecedor fornecedor,
            CancellationToken token
            )
        {
            _db.Remove(fornecedor);
            await SaveChangesAsync(token);
        }
        public async Task<Fornecedor?> GetByNome(
            string nome,
            CancellationToken token = default
            )
        {
            return await _db.Fornecedor
                 .Include(x => x.Produtos)
                .Include(x => x.Agendamentos)
                .FirstOrDefaultAsync(x => x.Nome == nome, token);
        }
        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }

        public async Task<Fornecedor?> GetByIdWithProdutosAsync(
            Guid id,
            CancellationToken token = default
            )
        {
            return await _db.Fornecedor
                .Include(x => x.Produtos)
                    .ThenInclude(x => x.LocalDescarga)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }
    }
}
