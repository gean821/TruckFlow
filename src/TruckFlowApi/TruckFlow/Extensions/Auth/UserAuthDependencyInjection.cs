using FluentValidation;
using TruckFlow.Application;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Interfaces;
using TruckFlow.Application.Validators.Fornecedor;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlowApi.Infra.Repositories;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Extensions.Auth
{
    public static class UserAuthDependencyInjection
    {
        public static IServiceCollection AddUserAuth(this IServiceCollection services)
        {
            services.AddTransient<IUsuarioService, UsuarioService>();
            services.AddTransient<IAuthService, AuthService>();

            return services;
        }
    }
}
