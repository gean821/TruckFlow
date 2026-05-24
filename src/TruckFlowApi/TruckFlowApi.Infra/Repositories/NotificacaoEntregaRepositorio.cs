using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class NotificacaoEntregaRepositorio : INotificacaoEntregaRepositorio
    {
        private readonly AppDbContext _db;

        public NotificacaoEntregaRepositorio(AppDbContext db)
        {
            _db = db;
        }

        public async Task<NotificacaoEntrega?> ClaimNextPushPendenteAsync(CancellationToken token = default)
        {
            return await _db.NotificacaoEntrega
                .FromSqlRaw(@"
                    SELECT *, xmin FROM ""NotificacaoEntrega""
                    WHERE ""Status"" = 0
                      AND ""Canal"" = 2
                      AND (""ProximaTentativaEm"" IS NULL OR ""ProximaTentativaEm"" <= NOW())
                    ORDER BY ""CreatedAt""
                    FOR UPDATE SKIP LOCKED
                    LIMIT 1")
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(token);
        }

        public async Task<List<NotificacaoEntrega>> ClaimLotePushPendenteReceiptAsync(
            int limit,
            TimeSpan idadeMinima,
            CancellationToken token = default)
        {
            var corteUltimaTentativa = DateTime.UtcNow.Subtract(idadeMinima);

            return await _db.NotificacaoEntrega
                .FromSqlInterpolated($@"
                    SELECT *, xmin FROM ""NotificacaoEntrega""
                    WHERE ""Status"" = 1
                      AND ""Canal"" = 2
                      AND ""ReceiptCheckedAt"" IS NULL
                      AND ""ProviderMessageId"" IS NOT NULL
                      AND ""UltimaTentativaEm"" IS NOT NULL
                      AND ""UltimaTentativaEm"" <= {corteUltimaTentativa}
                    ORDER BY ""UltimaTentativaEm""
                    FOR UPDATE SKIP LOCKED
                    LIMIT {limit}")
                .IgnoreQueryFilters()
                .ToListAsync(token);
        }

        public async Task<Notificacao?> GetNotificacaoByEntregaAsync(
            Guid entregaId,
            CancellationToken token = default)
        {
            return await _db.Notificacao
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(n => n.Entregas.Any(e => e.Id == entregaId))
                .FirstOrDefaultAsync(token);
        }

        public async Task MarcarReceiptVerificadoEmLoteAsync(
            IReadOnlyCollection<Guid> entregaIds,
            CancellationToken token = default)
        {
            if (entregaIds.Count == 0) {
                return;
            }   

            var ids = entregaIds.ToArray();
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE ""NotificacaoEntrega""
                   SET ""ReceiptCheckedAt"" = NOW()
                   WHERE ""Id"" = ANY({ids})",
                token);
        }

        public Task SaveChangesAsync(CancellationToken token = default)
            => _db.SaveChangesAsync(token);
    }
}