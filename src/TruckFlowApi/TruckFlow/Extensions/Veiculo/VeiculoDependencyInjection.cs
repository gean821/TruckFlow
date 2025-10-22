using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Veiculo;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Veiculo
{
    public static class VeiculoDependencyInjection
    {
        public static IServiceCollection AddProduto(this IServiceCollection services)
        {
            services.AddTransient<IVeiculoService, VeiculoService>();
            services.AddTransient<IVeiculoRepositorio, VeiculoRepositorio>();
            services.AddTransient<IMotoristaRepositorio, MotoristaRepositorio>();
            services.AddTransient<VeiculoFactory>();
            services.AddTransient<IValidator<VeiculoCreateDto>, VeiculoCreateValidator>();
            services.AddTransient<IValidator<VeiculoUpdateDto>, VeiculoUpdateValidator>();

            return services;
        }
    }
}
