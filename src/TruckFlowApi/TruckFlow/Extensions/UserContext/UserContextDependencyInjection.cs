using TruckFlow.Application;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.UserContext
{
    public static class UserContextDependencyInjection
    {
        public static IServiceCollection AddUserContext(this IServiceCollection services)
        {
            services.AddTransient<ICurrentUserService, CurrentUserService>();
            return services;
        }
    }
}

