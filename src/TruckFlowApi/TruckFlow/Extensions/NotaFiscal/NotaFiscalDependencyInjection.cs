using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.LocalDescarga;
using TruckFlow.Application.Validators.NotaFiscal;
using TruckFlow.Domain.Dto.LocalDescarga;
using TruckFlow.Domain.Dto.NotaFiscal;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.NotaFiscal
{
    public static class NotaFiscalDependencyInjection
    {
        public static IServiceCollection AddNotaFiscal (this IServiceCollection services)
        {
            services.AddTransient<INotaFiscalRepositorio, NotaFiscalRepositorio>();
            services.AddTransient<INotaFiscalService, NotaFiscalService>();
            services.AddTransient<IValidator<NotaFiscalParsedDto>, NotaFiscalParsedDtoValidator>();
            services.AddTransient<IValidator<NotaFiscalItemDto>, NotaFiscalItemDtoValidator>();

            return services;
        }
    }
}
