using TruckFlow.Application.AppMobile;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.AppMobile
{
    public static class AppMobileDependencyInjection
    {
        public static IServiceCollection AddAppMobile(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AppMobileOptions>(
                configuration.GetSection(AppMobileOptions.SectionName));

            services.AddTransient<IAppMobileService, AppMobileService>();

            return services;
        }
    }
}
