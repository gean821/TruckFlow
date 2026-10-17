using System.Threading.RateLimiting;
using TruckFlow.Middlewares;

namespace TruckFlow.Extensions.RateLimiting
{
    public static class AddRateLimitingDependencyInjection
    {
        private static readonly TimeSpan JanelaPorIp = TimeSpan.FromMinutes(1);
        private const int LimitePorIp = 5;

        private static readonly TimeSpan JanelaPorConta = TimeSpan.FromMinutes(5);
        private const int LimitePorConta = 5;

        private static readonly string[] CaminhosDeLogin =
        [
            "/v1/AuthAdmin/login",
            "/v1/AuthMotorista/login",
        ];
        public static IServiceCollection AddRateLimitingDependency(this IServiceCollection services)
        {
            services.AddTransient<LoginRateLimitKeyMiddleware>();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                var camadaPorIp = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    EhEndpointDeLogin(context)
                        ? RateLimitPartition.GetSlidingWindowLimiter(
                            ChavePorIp(context),
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = LimitePorIp,
                                Window = JanelaPorIp,
                                SegmentsPerWindow = 4,
                                QueueLimit = 0,
                            }
                        )
                        : RateLimitPartition.GetNoLimiter("sem-limite")
                );

                var camadaPorConta = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    EhEndpointDeLogin(context)
                        ? RateLimitPartition.GetSlidingWindowLimiter(
                            ChavePorConta(context),
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = LimitePorConta,
                                Window = JanelaPorConta,
                                SegmentsPerWindow = 5,
                                QueueLimit = 0,
                            }
                        )
                        : RateLimitPartition.GetNoLimiter("sem-limite")
                );

                options.GlobalLimiter = PartitionedRateLimiter.CreateChained(
                    camadaPorIp,
                    camadaPorConta
                );

                options.OnRejected = async (context, token) =>
                {
                    // O lease devolvido por um PartitionedRateLimiter combinado via
                    // CreateChained nem sempre carrega a metadata de RetryAfter do limiter
                    // interno que rejeitou (observado em teste: TryGetMetadata retorna false
                    // mesmo com SlidingWindowRateLimiter). Por isso sempre manda um
                    // Retry-After — com o valor da metadata quando disponível, caindo pra
                    // janela da camada por IP (a mais curta das duas) como estimativa segura.
                    var retryAfterSegundos = (int)JanelaPorIp.TotalSeconds;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        retryAfterSegundos = (int)retryAfter.TotalSeconds;
                    }

                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSegundos.ToString();
                    context.HttpContext.Response.ContentType = "application/json";
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new
                        {
                            success = false,
                            code = "RATE_LIMITED",
                            message = "Muitas tentativas. Aguarde um minuto e tente novamente.",
                        },
                        token
                    );
                };
            });

            return services;
        }

        public static WebApplication UseRateLimitingDependency(this WebApplication app)
        {
            app.UseMiddleware<LoginRateLimitKeyMiddleware>();
            app.UseRateLimiter();
            return app;
        }

        private static bool EhEndpointDeLogin(HttpContext context) =>
            HttpMethods.IsPost(context.Request.Method)
            && CaminhosDeLogin.Any(caminho =>
                context.Request.Path.StartsWithSegments(caminho, StringComparison.OrdinalIgnoreCase)
            );

        private static string ChavePorIp(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconhecido";

        private static string ChavePorConta(HttpContext context) =>
            context.Items[LoginRateLimitKeyMiddleware.ItemKey] as string ?? "sem-conta";
    }
}
