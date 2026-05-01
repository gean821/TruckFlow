using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Audit
{
    public static class AuditDependencyInjection
    {
        public static IServiceCollection AddAudit(this IServiceCollection services)
        {
            services.AddTransient<IAuditLogService, AuditLogService>();
            services.AddTransient<IAuditLogRepositorio, AuditLogRepositorio>();

            return services;
        }
    }
}
