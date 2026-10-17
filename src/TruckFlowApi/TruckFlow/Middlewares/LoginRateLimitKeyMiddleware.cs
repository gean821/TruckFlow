using System.Text.Json;

namespace TruckFlow.Middlewares
{
    public class LoginRateLimitKeyMiddleware : IMiddleware
    {
        public const string ItemKey = "LoginRateLimitUsuario";

        private static readonly string[] CaminhosDeLogin =
        {
            "/v1/AuthAdmin/login",
            "/v1/AuthMotorista/login",
        };

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (HttpMethods.IsPost(context.Request.Method) && EhCaminhoDeLogin(context))
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var corpo = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                var login = ExtrairLogin(corpo);
                if (!string.IsNullOrWhiteSpace(login))
                {
                    context.Items[ItemKey] = login.Trim().ToLowerInvariant();
                }
            }

            await next(context);
        }

        private static bool EhCaminhoDeLogin(HttpContext context) =>
            CaminhosDeLogin.Any(caminho =>
                context.Request.Path.StartsWithSegments(caminho, StringComparison.OrdinalIgnoreCase)
            );

        private static string? ExtrairLogin(string corpoJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(corpoJson);
                if (
                    doc.RootElement.TryGetProperty("login", out var prop)
                    && prop.ValueKind == JsonValueKind.String
                )
                {
                    return prop.GetString();
                }
            }
            catch (JsonException) { }

            return null;
        }
    }
}
