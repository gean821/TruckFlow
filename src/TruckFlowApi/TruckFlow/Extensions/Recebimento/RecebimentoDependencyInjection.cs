using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Recebimento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Recebimento
{
    public static class RecebimentoDependencyInjection
    {
        public static IServiceCollection AddPlanejamentoRecebimento(this IServiceCollection services)
        {
            services.AddTransient<IPlanejamentoRecebimentoService, PlanejamentoRecebimentoService>();
            services.AddTransient<IPlanejamentoRecebimentoRepositorio, PlanejamentoRecebimentoRepositorio>();
            services.AddTransient<RecebimentoFactory>();
            services.AddTransient<IValidator<RecebimentoCreateDto>, RecebimentoCreateDtoValidator>();
            services.AddTransient<IValidator<RecebimentoUpdateDto>, RecebimentoUpdateDtoValidator>();

            return services;
        }
    }
}
