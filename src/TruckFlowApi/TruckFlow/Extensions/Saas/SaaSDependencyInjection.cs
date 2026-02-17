using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Saas
{
    public static class SaaSDependencyInjection
    {
        public static IServiceCollection AddSaas(this IServiceCollection services)
        {
            services.AddTransient<ISaaSRegistrationService, SaaSRegistrationService>();
            return services;
        }
    }
}
