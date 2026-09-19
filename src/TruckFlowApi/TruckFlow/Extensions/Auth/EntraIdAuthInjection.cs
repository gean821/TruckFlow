using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using System.Security.Claims;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Extensions.Auth
{
    /// <summary>
    /// Login federado via Microsoft Entra ID — usado só pelos papéis "staff"
    /// (Admin/Gerente/Monitor/Portaria). Motorista nunca passa por aqui.
    ///
    /// Importante: isso NÃO substitui o JwtBearer já registrado por AddAuthenticationJwt.
    /// O esquema OIDC ("EntraId") e o Cookie que o acompanha só existem para segurar o
    /// handshake do login (challenge -> redirect -> callback). Depois disso, a API inteira
    /// continua protegida só pelo JWT próprio do TruckFlow — nunca pelo token do Entra ID.
    ///
    /// Ver Docs/entra-id-integracao-backlog.md (ENTRA-03) e
    /// Docs/entra-id-fluxo-diagrama.mmd para o desenho completo do fluxo.
    ///
    /// IMPORTANTE — nunca registrar sem ClientId configurado: o Microsoft.Identity.Web valida
    /// as opções do OIDC de forma eager na primeira vez que o AuthenticationMiddleware precisa
    /// checar TODOS os esquemas que implementam IAuthenticationRequestHandler (Cookie + OIDC
    /// incluídos) para saber se algum deles deve interceptar a requisição atual (ex.: é o
    /// callback /signin-oidc?) — isso acontece em TODA requisição, não só nas rotas do Entra ID.
    /// Com ClientId vazio essa validação lança IDW10106 e quebra a API inteira, não só o login
    /// federado. Por isso o registro abaixo só acontece quando há ClientId real configurado —
    /// sem credencial do Azure, o app roda 100% normal só com JWT/login local, e /entra/login
    /// falha isolado (não a API inteira) se alguém tentar usá-lo antes da hora.
    /// </summary>
    public static class EntraIdAuthInjection
    {
        public const string Scheme = "EntraId";
        public const string HandoffCookieScheme = "EntraIdHandoff";
        public const string UsuarioIdClaimType = "truckflow_usuario_id";

        public static WebApplicationBuilder AddAuthenticationEntraId(this WebApplicationBuilder builder)
        {
            var clientId = builder.Configuration["EntraIdOptions:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return builder;
            }

            var authBuilder = builder.Services.AddAuthentication();

            authBuilder.AddMicrosoftIdentityWebApp(
                builder.Configuration.GetSection("EntraIdOptions"),
                openIdConnectScheme: Scheme,
                cookieScheme: HandoffCookieScheme);

            builder.Services.Configure<CookieAuthenticationOptions>(HandoffCookieScheme, options =>
            {
                options.Cookie.Name = "tf_entra_handoff";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                options.SlidingExpiration = false;
            });

            builder.Services.Configure<OpenIdConnectOptions>(Scheme, options =>
            {
                options.ResponseType = "code";
                options.SaveTokens = false;

                var originalOnTokenValidated = options.Events.OnTokenValidated;
                options.Events.OnTokenValidated = async context =>
                {
                    if (originalOnTokenValidated is not null)
                        await originalOnTokenValidated(context);

                    var httpContext = context.HttpContext;
                    var logger = httpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("EntraIdAuthInjection");

                    var principal = context.Principal!;
                    // Microsoft.Identity.Web remapeia o claim curto "oid" pro schema longo abaixo —
                    // FindFirstValue("oid") não acha nada e cairia no fallback ClaimTypes.NameIdentifier,
                    // que na verdade é o claim "sub" (identificador por app, não o Object ID estável do
                    // usuário no tenant). Nunca usar "sub" aqui — ver aviso da Microsoft sobre chave de
                    // provisionamento em apps multi-tenant.
                    var oid = principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
                        ?? principal.FindFirstValue("oid");
                    var email = principal.FindFirstValue("email") ?? principal.FindFirstValue("preferred_username");
                    var nome = principal.FindFirstValue("name");
                    var groupIds = principal.FindAll("groups").Select(c => c.Value).ToList();

                    logger.LogInformation(
                        "Callback Entra ID recebido. oid={Oid} email={Email} groups={GroupCount} claims=[{Claims}]",
                        oid, email, groupIds.Count,
                        string.Join("; ", principal.Claims.Select(c => $"{c.Type}={c.Value}")));

                    if (string.IsNullOrEmpty(oid))
                    {
                        logger.LogError("Token do Entra ID sem claim 'oid' — não é possível provisionar sem identificador estável.");
                        context.Fail("Token do Entra ID sem claim 'oid'.");
                        return;
                    }

                    var jit = httpContext.RequestServices.GetRequiredService<IJitProvisioningService>();
                    var resultado = await jit.SincronizarAsync(oid, email, nome, groupIds, httpContext.RequestAborted);

                    if (!resultado.Success)
                    {
                        httpContext.Items["entra_falha"] = resultado.FailureReason?.ToString() ?? "Desconhecida";
                        context.Fail($"Provisionamento rejeitado: {resultado.FailureReason}");
                        return;
                    }

                    ((ClaimsIdentity)principal.Identity!).AddClaim(
                        new Claim(UsuarioIdClaimType, resultado.Usuario!.Id.ToString()));
                };

                var originalOnAuthenticationFailed = options.Events.OnAuthenticationFailed;
                options.Events.OnAuthenticationFailed = async context =>
                {
                    if (originalOnAuthenticationFailed is not null)
                        await originalOnAuthenticationFailed(context);

                    var motivo = context.HttpContext.Items["entra_falha"] as string ?? "Erro";
                    var frontendBase = context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>()["EntraIdOptions:FrontendLoginUrl"]
                        ?? "/login";

                    context.Response.Redirect($"{frontendBase}?ssoErro={Uri.EscapeDataString(motivo)}");
                    context.HandleResponse();
                };

                var originalOnRemoteFailure = options.Events.OnRemoteFailure;
                options.Events.OnRemoteFailure = context =>
                {
                    originalOnRemoteFailure?.Invoke(context);

                    var frontendBase = context.HttpContext.RequestServices
                        .GetRequiredService<IConfiguration>()["EntraIdOptions:FrontendLoginUrl"]
                        ?? "/login";

                    context.Response.Redirect($"{frontendBase}?ssoErro=CanceladoOuFalhou");
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });

            return builder;
        }
    }
}
