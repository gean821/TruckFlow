using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class NotificacaoRepositorio : INotificacaoRepositorio
    {
        private readonly AppDbContext _db;

        public NotificacaoRepositorio(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Notificacao>> ListByUserAsync(
            Guid userId,
            int skip,
            int take,
            CancellationToken token = default)
        {
            return await _db.Notificacao
                .AsNoTracking()
                .Where(n => n.DestinatarioUsuarioId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(token);
        }

        public async Task<int> CountUnreadByUserAsync(
            Guid userId,
            CancellationToken token = default)
        {
            return await _db.Notificacao
                .AsNoTracking()
                .Where(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null)
                .CountAsync(token);
        }

        public async Task<Notificacao?> GetByIdForUserAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default)
        {
            return await _db.Notificacao
                .FirstOrDefaultAsync(
                    n => n.Id == notificacaoId && n.DestinatarioUsuarioId == userId,
                    token);
        }

        public async Task<Notificacao> UpdateAsync(
            Notificacao notificacao,
            CancellationToken token = default)
        {
            _db.Notificacao.Update(notificacao);
            await _db.SaveChangesAsync(token);
            return notificacao;
        }
    }
}