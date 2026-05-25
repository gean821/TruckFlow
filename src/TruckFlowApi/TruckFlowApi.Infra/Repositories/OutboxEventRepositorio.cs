using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class OutboxEventRepositorio : IOutboxEventRepositorio
    {
        private const int MaxTentativas = 8;

        private readonly AppDbContext _db;

        public OutboxEventRepositorio(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OutboxStatsDto> GetStatsAsync(CancellationToken token = default)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            var pending = await _db.OutboxEvent
                .CountAsync(o => o.ProcessedAt == null, token);

            var failedDefinitive = await _db.OutboxEvent
                .CountAsync(o =>
                    o.ProcessedAt == null
                    && o.Tentativas >= MaxTentativas
                    && o.ProximaTentativaEm == null, token);

            var processedLastHour = await _db.OutboxEvent
                .CountAsync(o => o.ProcessedAt != null && o.ProcessedAt >= oneHourAgo, token);

            double? averageLagMs = null;

            if (processedLastHour > 0)
            {
                var lag = await _db.Database
                    .SqlQueryRaw<double?>(@"
                        SELECT AVG(EXTRACT(EPOCH FROM (""ProcessedAt"" - ""CreatedAt"")) * 1000)::float8
                        FROM ""OutboxEvent""
                        WHERE ""ProcessedAt"" IS NOT NULL
                          AND ""ProcessedAt"" >= {0}",
                        oneHourAgo)
                    .FirstOrDefaultAsync(token);

                averageLagMs = lag;
            }

            return new OutboxStatsDto(pending, failedDefinitive, processedLastHour, averageLagMs);
        }
    }
}