using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TruckFlow.Application.Sefaz
{
    public static class SefazDependencyInjection
    {
        public static IServiceCollection AddSefaz(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SefazOptions>(configuration.GetSection(SefazOptions.SectionName));

            var section = configuration.GetSection(SefazOptions.SectionName);
            var useFake = section.GetValue<bool?>(nameof(SefazOptions.UseFake)) ?? true;

            if (useFake)
            {
                services.AddTransient<ISefazClient, FakeSefazClient>();
            }
            else
            {
                services.AddTransient<ISefazClient, ZeusSefazClient>();
            }

            return services;
        }
    }
}
