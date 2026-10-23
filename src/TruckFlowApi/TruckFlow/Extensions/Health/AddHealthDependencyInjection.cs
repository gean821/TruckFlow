using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TruckFlow.Extensions.Health
{
    public static class AddHealthDependencyInjection
    {
        public const string BasePath = "/health";
        public const string LivePath = "/health/live";
        public const string ReadyPath = "/health/ready";

        private const string LiveTag = "live";
        private const string ReadyTag = "ready";

        public static IServiceCollection AddHealthDependency(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy("API no ar."), tags: new[] { LiveTag })
                .AddCheck<DatabaseHealthCheck>("postgres", tags: new[] { ReadyTag });

            return services;
        }
        public static WebApplication MapHealthEndpoints(this WebApplication app)
        {
            app.MapHealthChecks(BasePath).AllowAnonymous();

            app.MapHealthChecks(LivePath, new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(LiveTag)
            }).AllowAnonymous();

            app.MapHealthChecks(ReadyPath, new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(ReadyTag)
            }).AllowAnonymous();

            return app;
        }
    }
}
