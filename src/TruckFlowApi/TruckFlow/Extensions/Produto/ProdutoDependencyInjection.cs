using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Dto.Produto;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Produto;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Produto
{
    public static class ProdutoDependencyInjection
    {
        public static IServiceCollection AddProduto(this IServiceCollection services)
        {
            services.AddTransient<IProdutoService, ProdutoService>();
            services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();
            services.AddTransient<ILocalDescargaRepositorio, LocalDescargaRepositorio>();
            services.AddTransient<ProdutoFactory>();

            services.AddTransient<IValidator<ProdutoCreateDto>, ProdutoCreateValidator>();
            services.AddTransient<IValidator<ProdutoEditDto>, ProdutoEditValidator>();

            return services;
        }
    }
}
