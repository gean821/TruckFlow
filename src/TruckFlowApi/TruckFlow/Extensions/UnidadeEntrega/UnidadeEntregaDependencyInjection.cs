using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Dto.UnidadeEntrega;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.UnidadeEntrega;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.UnidadeEntrega
{
    public static class UnidadeEntregaDependencyInjection
    {
        public static IServiceCollection AddUnidadeEntrega(this IServiceCollection services)
        {
            services.AddTransient<IUnidadeEntregaService, UnidadeEntregaService>();
            services.AddTransient<IUnidadeEntregaRepositorio, UnidadeEntregaRepositorio>();
            services.AddTransient<UnidadeEntregaFactory>();
            services.AddTransient<IValidator<UnidadeEntregaCreateDto>, UnidadeEntregaCreateValidator>();
            services.AddTransient<IValidator<UnidadeEntregaUpdateDto>, UnidadeEntregaUpdateValidator>();

            return services;
        }
    }
}
