using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Dto.Fornecedor;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Fornecedor;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Fornecedor
{
    public static class FornecedorDependencyInjection
    {
        public static IServiceCollection AddFornecedor(this IServiceCollection services)
        {
            services.AddTransient<IFornecedorService, FornecedorService>();       
            services.AddTransient<IFornecedorRepositorio, FornecedorRepositorio>();       
            services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();       
            services.AddTransient<FornecedorFactory>();       
            services.AddTransient<IValidator<FornecedorCreateDto>, FornecedorCreateValidator>();       
            services.AddTransient<IValidator<FornecedorUpdateDto>, FornecedorUpdateValidator>();     
            
            return services;
        }
    }
}
