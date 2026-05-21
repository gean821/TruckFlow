using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;
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

        public async Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedAsync(
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token = default)
        {
            var baseQuery = _db.Notificacao
                .AsNoTracking()
                .Where(n => n.DestinatarioUsuarioId == userId);

            if (query.UnreadOnly == true)
            {
                baseQuery = baseQuery.Where(n => n.LidaEm == null);
            }

            if (query.Tipo.HasValue)
            {
                var tipo = (TipoNotificacao)query.Tipo.Value;
                baseQuery = baseQuery.Where(n => n.Tipo == tipo);
            }

            if (query.Prioridade.HasValue)
            {
                var prio = (PrioridadeNotificacao)query.Prioridade.Value;
                baseQuery = baseQuery.Where(n => n.Prioridade == prio);
            }

            var totalCount = await baseQuery.CountAsync(token);

            var items = await baseQuery
                .OrderByDescending(n => n.CreatedAt)
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(token);

            return (items, totalCount);
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