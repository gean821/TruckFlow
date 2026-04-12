

using Microsoft.Extensions.DependencyInjection;
namespace TruckFlow.Extensions.Cors
{
    public static class AddCorsDependencyInjection
    {
        public static IServiceCollection AddCorsDependency(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(
                             "http://localhost:5173",
                             "https://truck-flow-app.vercel.app" 
                         )
                         .AllowAnyHeader()
                         .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
