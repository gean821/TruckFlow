using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.LocalDescarga;
using TruckFlow.Domain.Dto.LocalDescarga;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Motorista
{
    public static class MotoristaDependencyInjection
    {
        public static IServiceCollection AddMotorista(this IServiceCollection services)
        {
            services.AddTransient<IMotoristaRepositorio, MotoristaRepositorio>();
            services.AddTransient<IMotoristaService, MotoristaService>();
            return services;
        }
    }
}
