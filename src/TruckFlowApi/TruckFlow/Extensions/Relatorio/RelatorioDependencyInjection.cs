using TruckFlow.Application;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.Relatorio
{
    public static class RelatorioDependencyInjection
    {
        public static IServiceCollection AddRelatorio(this IServiceCollection services)
        {
            services.AddTransient<IRelatorioService, RelatorioService>();
            return services;
        }
    }
}
