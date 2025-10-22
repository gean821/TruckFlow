using FluentValidation;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application;
using TruckFlow.Application.Validators.LocalDescarga;
using TruckFlow.Application.Validators.Produto;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Domain.Dto.LocalDescarga;

namespace TruckFlow.Extensions.LocalDescarga
{
    public static class LocalDescargaDependencyInjection
    {
        public static IServiceCollection AddLocalDescarga(this IServiceCollection services)
        {
            services.AddTransient<ILocalDescargaRepositorio, LocalDescargaRepositorio>();
            services.AddTransient<ILocalDescargaService, LocalDescargaService>();
            services.AddTransient<IValidator<LocalDescargaCreateDto>, LocalDescargaCreateValidator>();
            services.AddTransient<IValidator<LocalDescargaUpdateDto>, LocalDescargaUpdateValidator>();

            return services;
        }
    }
}
