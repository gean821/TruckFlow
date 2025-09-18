

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
                    policy.WithOrigins("http://localhost:5173") // endereço do front atual no localhost!
                    .AllowAnyHeader()
                    .AllowAnyMethod();
                });
            });

            return services;
        }
    }
}
