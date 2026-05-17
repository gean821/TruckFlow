namespace TruckFlow.Extensions.Cors
{
    public static class AddCorsDependencyInjection
    {
        public const string PolicyName = "AllowFrontend";

        private static readonly string[] FallbackOrigins =
        {
            "http://localhost:5173",
            "https://truck-flow-app.vercel.app",
            "http://192.168.18.6:8080"
        };

        public static IServiceCollection AddCorsDependency(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
            
            if (origins is null || origins.Length == 0) {
                origins = FallbackOrigins;
            }

            services.AddCors(options =>
            {
                options.AddPolicy(PolicyName, policy =>
                {
                    policy.WithOrigins(origins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}