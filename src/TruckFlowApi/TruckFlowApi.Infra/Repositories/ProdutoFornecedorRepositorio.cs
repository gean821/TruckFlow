using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class ProdutoFornecedorRepositorio : IProdutoFornecedorRepositorio
    {
        private readonly AppDbContext _db;

        public ProdutoFornecedorRepositorio(AppDbContext db) => _db = db;

        public Task<ProdutoFornecedor?> GetByFornecedorAndCodigo(
            Guid fornecedorId,
            string codigoFornecedor,
            CancellationToken token = default) =>
            _db.Set<ProdutoFornecedor>()
                .Include(x => x.Produto)
                .FirstOrDefaultAsync(
                    x => x.FornecedorId == fornecedorId
                         && x.CodigoFornecedor == codigoFornecedor
                         && x.DeletedAt == null,
                    token);

        public Task<ProdutoFornecedor?> GetByProdutoAndFornecedor(
            Guid produtoId,
            Guid fornecedorId,
            CancellationToken token = default) =>
            _db.Set<ProdutoFornecedor>()
                .FirstOrDefaultAsync(
                    x => x.ProdutoId == produtoId
                         && x.FornecedorId == fornecedorId
                         && x.DeletedAt == null,
                    token);

        public async Task UpsertMapping(
            Guid empresaId,
            Guid fornecedorId,
            Guid produtoId,
            string? codigoFornecedor,
            string? eanFornecedor,
            CancellationToken token = default)
        {
            var existing = await GetByProdutoAndFornecedor(produtoId, fornecedorId, token);
            var now = DateTime.UtcNow;

            if (existing is null)
            {
                var produto = await _db.Produto.FirstAsync(p => p.Id == produtoId, token);
                var fornecedor = await _db.Fornecedor.FirstAsync(f => f.Id == fornecedorId, token);
                var empresa = await _db.Empresa.FirstAsync(e => e.Id == empresaId, token);

                _db.Set<ProdutoFornecedor>().Add(new ProdutoFornecedor
                {
                    ProdutoId = produtoId,
                    FornecedorId = fornecedorId,
                    EmpresaId = empresaId,
                    Produto = produto,
                    Fornecedor = fornecedor,
                    Empresa = empresa,
                    CodigoFornecedor = codigoFornecedor,
                    EanFornecedor = eanFornecedor,
                    CreatedAt = now
                });
            }
            else
            {
                bool changed = false;
                if (!string.IsNullOrWhiteSpace(codigoFornecedor) &&
                    existing.CodigoFornecedor != codigoFornecedor)
                {
                    existing.CodigoFornecedor = codigoFornecedor;
                    changed = true;
                }
                if (!string.IsNullOrWhiteSpace(eanFornecedor) &&
                    existing.EanFornecedor != eanFornecedor)
                {
                    existing.EanFornecedor = eanFornecedor;
                    changed = true;
                }
                if (changed)
                {
                    existing.UpdatedAt = now;
                }
            }
        }

        public Task SaveChangesAsync(CancellationToken token = default) =>
            _db.SaveChangesAsync(token);
    }
}