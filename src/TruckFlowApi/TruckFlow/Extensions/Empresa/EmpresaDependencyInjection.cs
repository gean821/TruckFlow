using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Empresa
{
    public static class EmpresaDependencyInjection
    {
        public static IServiceCollection AddEmpresa(this IServiceCollection services)
        {
            //services.AddTransient<IEmpresaService, EmpresaService>();
            services.AddTransient<IEmpresaRepositorio, EmpresaRepositorio>();
            services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();
            //services.AddTransient<IValidator<EmpresaCreateDto>, EmpresaCreateValidator>();
            //services.AddTransient<IValidator<EmpresaUpdateDto>, EmpresaUpdateValidator>();

            return services;
        }
    }
}
