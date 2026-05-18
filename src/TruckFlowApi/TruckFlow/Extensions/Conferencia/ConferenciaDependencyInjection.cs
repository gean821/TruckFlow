using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Conferencia
{
    public static class ConferenciaDependencyInjection
    {
        public static IServiceCollection AddConferencia(this IServiceCollection services)
        {
            services.AddTransient<INotaFiscalItemRepositorio, NotaFiscalItemRepositorio>();
            services.AddTransient<IConferenciaService, ConferenciaService>();

            return services;
        }
    }
}