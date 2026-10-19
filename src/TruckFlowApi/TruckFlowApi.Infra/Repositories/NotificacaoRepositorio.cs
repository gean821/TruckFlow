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

            if (query.DataInicio.HasValue)
            {
                baseQuery = baseQuery.Where(n => n.CreatedAt >= query.DataInicio.Value);
            }

            if (query.DataFim.HasValue)
            {
                baseQuery = baseQuery.Where(n => n.CreatedAt <= query.DataFim.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var termo = $"%{query.Search.Trim()}%";
                baseQuery = baseQuery.Where(n =>
                    EF.Functions.ILike(n.Titulo, termo) || EF.Functions.ILike(n.Corpo, termo));
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

            // Mensagens com fan-out (ex.: motorista → todos os admins da empresa) compartilham
            // o mesmo CorrelationId; o DISTINCT ON abaixo colapsa cada grupo em uma única linha
            // representante, evitando que a mesma mensagem apareça duplicada na conversa.
            // Notificações sem fan-out não têm CorrelationId, então cada uma é seu próprio grupo (por Id).
            var query = _db.Notificacao
                .FromSqlInterpolated($@"
                    SELECT * FROM (
                        SELECT DISTINCT ON (COALESCE(""CorrelationId""::text, ""Id""::text)) *, xmin
                        FROM ""Notificacao""
                        WHERE ""Payload""::jsonb->>'agendamentoId' = {agendamentoIdString}
                          AND (
                            ""DestinatarioUsuarioId"" = {userId}
                            OR ""Payload""::jsonb->>'autorUsuarioId' = {userIdString}
                          )
                        ORDER BY COALESCE(""CorrelationId""::text, ""Id""::text), ""CreatedAt"" ASC, ""Id"" ASC
                    ) grouped
                    ORDER BY grouped.""CreatedAt"" ASC")
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

        public Task<int> MarkAllAsReadByUserAsync(
            Guid userId,
            CancellationToken token = default) =>
            MarkAllAsReadCoreAsync(_db.Notificacao, userId, token);

        public Task<int> MarkAllAsReadByUserAcrossTenantsAsync(
            Guid userId,
            CancellationToken token = default) =>
            MarkAllAsReadCoreAsync(_db.Notificacao.IgnoreQueryFilters(), userId, token);

        private static Task<int> MarkAllAsReadCoreAsync(
            IQueryable<Notificacao> source,
            Guid userId,
            CancellationToken token)
        {
            var now = DateTime.UtcNow;

            return source
                .Where(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(n => n.LidaEm, now)
                    .SetProperty(n => n.UpdatedAt, now), token);
        }
    }
}