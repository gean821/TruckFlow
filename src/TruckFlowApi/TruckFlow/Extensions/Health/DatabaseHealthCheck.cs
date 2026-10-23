using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TruckFlowApi.Infra.Database;

namespace TruckFlow.Extensions.Health
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _context;

        public DatabaseHealthCheck(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Postgres acessivel.")
                    : HealthCheckResult.Unhealthy("Postgres inacessivel.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Falha ao consultar o Postgres.", ex);
            }
        }
    }
}
