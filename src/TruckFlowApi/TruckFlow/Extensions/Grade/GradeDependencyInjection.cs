using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Grade;
using TruckFlow.Domain.Dto.Grade;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Grade
{
    public static class GradeDependencyInjection
    {
        public static IServiceCollection AddGrade(this IServiceCollection services)
        {
            services.AddTransient<IGradeService, GradeService>();
            services.AddTransient<IGradeRepositorio, GradeRepositorio>();
            services.AddTransient<IFornecedorRepositorio, FornecedorRepositorio>();
            services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();
            services.AddTransient<GradeFactory>();
            services.AddTransient<IValidator<GradeCreateDto>, GradeCreateValidator>();
            services.AddTransient<IValidator<GradeUpdateDto>, GradeUpdateValidator>();

            return services;
        }
    }
}
