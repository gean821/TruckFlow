using Microsoft.Extensions.DependencyInjection.Extensions;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.RelatoriosBi;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Relatorio
{
    public static class RelatorioDependencyInjection
    {
        public static IServiceCollection AddRelatorio(this IServiceCollection services)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddTransient<IRelatorioService, RelatorioService>();
            services.AddTransient<IRelatorioBiRepositorio, RelatorioBiRepositorio>();
            services.AddTransient<IRelatorioBiService, RelatorioBiService>();
            return services;
        }
    }
}
