using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.AgendamentoMotorista;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Dashboard
{
    public static class DashboardDependencyInjection
    {
        public static IServiceCollection AddDashboard(this IServiceCollection services)
        {
            services.AddScoped<IDashboardRepositorio, DashboardRepositorio>();
            services.AddScoped<IDashboardService, DashboardService>();
            return services;
        }
    }
}
