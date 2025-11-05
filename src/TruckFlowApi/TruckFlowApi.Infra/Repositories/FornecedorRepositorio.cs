using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class FornecedorRepositorio : IFornecedorRepositorio
    {
        private readonly AppDbContext _db;

        public FornecedorRepositorio(AppDbContext db) => _db = db;

        public async Task<Fornecedor> CreateFornecedor(Fornecedor fornecedor, CancellationToken token = default)
        { 
            await _db.Fornecedor.AddAsync(fornecedor, token);
            return fornecedor;
        }

        public async Task<List<Fornecedor>> GetAll(CancellationToken token = default)
        {
            return await _db.Fornecedor
                .Include(x=> x.Produtos)
                    .ThenInclude(x=> x.LocalDescarga)
                .Include(x => x.NotaFiscal)
                .Include(x => x.Agendamento)
                .ToListAsync(token);
        }

        public async Task<Fornecedor> GetById(Guid id, CancellationToken token = default)
        {
            var fornecedor = await _db.Fornecedor
                .Include(x => x.NotaFiscal)
                .Include(x => x.Agendamento)
                .FirstOrDefaultAsync(x => x.Id == id, token);

            return fornecedor!;
        }

        public async Task<Fornecedor> Update(Guid id, Fornecedor fornecedor, CancellationToken token = default)
        {
            var fornecedorEncontrado = await GetById(id, token);

            fornecedorEncontrado.Id = fornecedor.Id;
            fornecedorEncontrado.Nome = fornecedor.Nome;
            fornecedorEncontrado.NotaFiscal = fornecedor.NotaFiscal;
            fornecedorEncontrado.Agendamento = fornecedor.Agendamento;

            await SaveChangesAsync(token);
            return fornecedorEncontrado;
        }
           
        public async Task Delete(Guid id, CancellationToken token)
        {
            var fornecedorEncontrado = await GetById(id, token);

            _db.Remove(fornecedorEncontrado);
            await SaveChangesAsync(token);
        }
        public async Task<Fornecedor> GetByNome(string nome, CancellationToken token = default)
        {
            var fornecedor = await _db.Fornecedor
                .Include(x => x.NotaFiscal)
                .Include(x => x.Agendamento)
                .FirstOrDefaultAsync(x => x.Nome == nome, token);

            return fornecedor!;
        }
        public async Task SaveChangesAsync(CancellationToken token = default)
        {
            await _db.SaveChangesAsync(token);
        }

        public async Task<Fornecedor?> GetByIdWithProdutosAsync(Guid id, CancellationToken token = default)
        {
            return await _db.Fornecedor
                .Include(x => x.Produtos)
                    .ThenInclude(x=> x.LocalDescarga)
                .FirstOrDefaultAsync(x => x.Id == id, token);
        }
    }
}
