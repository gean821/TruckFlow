using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.AgendamentoMotorista;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Agendamento
{
    public static class AgendamentoDependencyInjection
    {
        public static IServiceCollection AddAgendamento(this IServiceCollection services)
        {
            services.AddTransient<IAgendamentoAdminService, AgendamentoAdminService>();
            services.AddTransient<IAgendamentoRepositorio, AgendamentoRepositorio>();
            services.AddTransient<IValidator<AgendamentoAdminCreateDto>, AgendamentoAdminCreateValidator>();
            services.AddTransient<IAgendamentoMotoristaService, AgendamentoMotoristaService>();
            services.AddTransient<IValidator<AgendamentoAdminUpdateDto>, AgendamentoAdminUpdateDtoValidator>();
            services.AddTransient<IValidator<AgendamentoAdminUpdateDto>, AgendamentoAdminUpdateDtoValidator>();
                
            return services;
        }
    }
}
