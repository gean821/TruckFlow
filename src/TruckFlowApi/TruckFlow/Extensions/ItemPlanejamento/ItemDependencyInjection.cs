using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.ItemPlanejamento;
using TruckFlow.Application.Validators.Recebimento;
using TruckFlow.Domain.Dto.ItensPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.ItemPlanejamento
{
    public static class ItemDependencyInjection
    {
        public static IServiceCollection AddItemPlanejameto(this IServiceCollection services)
        {
            services.AddTransient<IItemPlanejamentoService, ItemPlanejamentoService>();
            services.AddTransient<IItemPlanejamentoRepositorio, ItemPlanejamentoRepositorio>();
            services.AddTransient<ItemPlanejamentoFactory>();
            services.AddTransient<IValidator<ItemPlanejamentoCreateDto>, ItemPlanejamentoCreateDtoValidator>();
            services.AddTransient<IValidator<ItemPlanejamentoUpdateDto>, ItemPlanejamentoUpdateDtoValidator>();

            return services;
        }
    }
}
