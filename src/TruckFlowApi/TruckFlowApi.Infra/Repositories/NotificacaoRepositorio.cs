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

        public Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedAsync(
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token = default) =>
            ListByUserPagedCoreAsync(_db.Notificacao.AsNoTracking(), userId, query, token);

        public Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedAcrossTenantsAsync(
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token = default) =>
            ListByUserPagedCoreAsync(_db.Notificacao.AsNoTracking().IgnoreQueryFilters(), userId, query, token);

        private static async Task<(List<Notificacao> Items, int TotalCount)> ListByUserPagedCoreAsync(
            IQueryable<Notificacao> source,
            Guid userId,
            NotificacaoListQueryDto query,
            CancellationToken token)
        {
            var baseQuery = source.Where(n => n.DestinatarioUsuarioId == userId);

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

        public Task<int> CountUnreadByUserAsync(
            Guid userId,
            CancellationToken token = default) =>
            _db.Notificacao
                .AsNoTracking()
                .Where(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null)
                .CountAsync(token);

        public Task<int> CountUnreadByUserAcrossTenantsAsync(
            Guid userId,
            CancellationToken token = default) =>
            _db.Notificacao
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null)
                .CountAsync(token);

        public Task<Notificacao?> GetByIdForUserAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default) =>
            _db.Notificacao
                .FirstOrDefaultAsync(
                    n => n.Id == notificacaoId && n.DestinatarioUsuarioId == userId,
                    token);

        public Task<Notificacao?> GetByIdForUserAcrossTenantsAsync(
            Guid notificacaoId,
            Guid userId,
            CancellationToken token = default) =>
            _db.Notificacao
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    n => n.Id == notificacaoId && n.DestinatarioUsuarioId == userId,
                    token);

        public Task<List<Notificacao>> ListByUserAndAgendamentoAsync(
            Guid userId,
            Guid agendamentoId,
            CancellationToken token = default) =>
            ListByUserAndAgendamentoCoreAsync(applyFilters: true, userId, agendamentoId, token);

        public Task<List<Notificacao>> ListByUserAndAgendamentoAcrossTenantsAsync(
            Guid userId,
            Guid agendamentoId,
            CancellationToken token = default) =>
            ListByUserAndAgendamentoCoreAsync(applyFilters: false, userId, agendamentoId, token);

        private Task<List<Notificacao>> ListByUserAndAgendamentoCoreAsync(
            bool applyFilters,
            Guid userId,
            Guid agendamentoId,
            CancellationToken token)
        {
            var agendamentoIdString = agendamentoId.ToString();
            var userIdString = userId.ToString();

            var query = _db.Notificacao
                .FromSqlInterpolated($@"
                    SELECT *, xmin
                    FROM ""Notificacao""
                    WHERE ""Payload""::jsonb->>'agendamentoId' = {agendamentoIdString}
                      AND (
                        ""DestinatarioUsuarioId"" = {userId}
                        OR ""Payload""::jsonb->>'autorUsuarioId' = {userIdString}
                      )
                    ORDER BY ""CreatedAt"" ASC")
                .AsNoTracking();

            if (!applyFilters)
            {
                query = query.IgnoreQueryFilters();
            }

            return query.ToListAsync(token);
        }

        public async Task<Notificacao> UpdateAsync(
            Notificacao notificacao,
            CancellationToken token = default)
        {
            _db.Notificacao.Update(notificacao);
            await _db.SaveChangesAsync(token);
            return notificacao;
        }

        public async Task<Notificacao> AddAsync(
            Notificacao notificacao,
            CancellationToken token = default)
        {
            await _db.Notificacao.AddAsync(notificacao, token);
            await _db.SaveChangesAsync(token);
            return notificacao;
        }

        public async Task AddRangeAsync(
            IEnumerable<Notificacao> notificacoes,
            CancellationToken token = default)
        {
            await _db.Notificacao.AddRangeAsync(notificacoes, token);
            await _db.SaveChangesAsync(token);
        }
    }
}