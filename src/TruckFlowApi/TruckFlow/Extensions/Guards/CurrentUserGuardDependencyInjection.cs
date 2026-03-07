using TruckFlow.Application;

namespace TruckFlow.Extensions.Guards
{
    public static class CurrentUserGuardDependencyInjection
    {
        public static IServiceCollection AddSecurity(this IServiceCollection services)
        {
            services.AddScoped<CurrentUserGuard>();
            return services;
        }
    }
}
