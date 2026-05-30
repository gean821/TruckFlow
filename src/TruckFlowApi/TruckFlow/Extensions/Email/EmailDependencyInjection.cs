using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Contracts;
using TruckFlowApi.Infra.Email;
using TruckFlowApi.Infra.Repositories;

namespace TruckFlow.Extensions.Email
{
    public static class EmailDependencyInjection
    {
        public static IServiceCollection AddEmail(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ResendOptions>(configuration.GetSection("Resend"));
            services.AddHttpClient<IEmailService, ResendEmailService>();
            services.AddScoped<IVerificacaoEmailService, VerificacaoEmailService>();
            services.AddScoped<ICodigoVerificacaoEmailRepositorio, CodigoVerificacaoEmailRepositorio>();

            return services;
        }
    }
}
