using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.RecebimentoEvento
{
    public static class RecebimentoEventoDependencyInjection
    {
        public static IServiceCollection AddRecebimentoEvento(this IServiceCollection services)
        {
            services.AddTransient<IRecebimentoEventoService, RecebimentoService>();
            services.AddTransient<IRecebimentoEventoRepositorio, RecebimentoEventoRepositorio>();

            return services;
        }
    }
}
