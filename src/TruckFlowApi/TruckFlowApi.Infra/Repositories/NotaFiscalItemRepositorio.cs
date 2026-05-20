using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class NotaFiscalItemRepositorio : INotaFiscalItemRepositorio
    {
        private readonly AppDbContext _db;

        public NotaFiscalItemRepositorio(AppDbContext db) => _db = db;

        public async Task<NotaFiscalItem?> GetByIdAsync(Guid id, CancellationToken token = default) =>
            await _db.NotaFiscalItems
                .Include(x => x.NotaFiscal)
                    .ThenInclude(n => n.Fornecedor)
                .Include(x => x.Produto)
                .FirstOrDefaultAsync(x => x.Id == id, token);

        public Task SaveChangesAsync(CancellationToken token = default) =>
            _db.SaveChangesAsync(token);
    }
}