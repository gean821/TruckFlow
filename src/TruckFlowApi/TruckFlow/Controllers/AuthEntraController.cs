using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TruckFlow.Application.Interfaces;
using TruckFlow.Extensions.Auth;
using TruckFlowApi.Infra.Database;

namespace TruckFlow.Controllers
{
    /// <summary>
    /// Login federado (SSO) via Microsoft Entra ID — só para os papéis staff
    /// (Admin/Gerente/Monitor/Portaria). Motorista continua 100% no login local
    /// (AuthMotoristaController), sem nenhuma relação com este controller.
    /// </summary>
    [ApiController]
    [Route("v1/AuthAdmin/entra")]
    public class AuthEntraController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IAuthService _authService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthEntraController> _logger;

        public AuthEntraController(
            AppDbContext db,
            IAuthService authService,
            IRefreshTokenService refreshTokenService,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<AuthEntraController> logger)
        {
            _db = db;
            _authService = authService;
            _refreshTokenService = refreshTokenService;
            _env = env;
            _configuration = configuration;
            _logger = logger;
        }

        private string FrontendLoginUrl =>
            _configuration["EntraIdOptions:FrontendLoginUrl"] ?? "/login";

        /// <summary>
        /// Dispara o challenge do Entra ID. O usuário volta autenticado (via cookie-ponte de
        /// curta duração) direto em /entra/callback — a lógica de provisionamento (JIT) já
        /// roda dentro do OnTokenValidated (ver EntraIdAuthInjection), antes de chegar aqui.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action(nameof(Callback), "AuthEntra"),
            };

            return Challenge(properties, EntraIdAuthInjection.Scheme);
        }

        /// <summary>
        /// Ponto de chegada depois de um login Entra ID bem-sucedido e já provisionado.
        /// Emite o JWT + refresh token do TruckFlow reaproveitando o mesmo AuthService/
        /// RefreshTokenService do login local, a partir daqui os dois mundos são idênticos.
        /// </summary>
        [HttpGet("callback")]
        [Authorize(AuthenticationSchemes = EntraIdAuthInjection.HandoffCookieScheme)]
        public async Task<IActionResult> Callback(CancellationToken token = default)
        {
            var usuarioIdClaim = User.FindFirst(EntraIdAuthInjection.UsuarioIdClaimType)?.Value;
            if (usuarioIdClaim is null || !Guid.TryParse(usuarioIdClaim, out var usuarioId))
            {
                _logger.LogError("Callback Entra ID sem claim de usuário resolvido — não deveria acontecer.");
                await HttpContext.SignOutAsync(EntraIdAuthInjection.HandoffCookieScheme);
                return Redirect($"{FrontendLoginUrl}?ssoErro=Interno");
            }

            var usuario = await _db.Users
                .Include(u => u.Administrador)
                .FirstOrDefaultAsync(u => u.Id == usuarioId, token);

            await HttpContext.SignOutAsync(EntraIdAuthInjection.HandoffCookieScheme);

            if (usuario is null)
            {
                _logger.LogError("Callback Entra ID: UsuarioId {UsuarioId} resolvido no JIT mas não encontrado no banco.", usuarioId);
                return Redirect($"{FrontendLoginUrl}?ssoErro=Interno");
            }

            var accessToken = await _authService.GenerateTokenAsync(usuario, token);
            var refresh = await _refreshTokenService.IssueAsync(
                usuario.Id,
                usuario.EmpresaId,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                ct: token);

            RefreshCookieHelper.SetRefresh(Response, _env, refresh.RawToken, refresh.ExpiresAt);

            _logger.LogInformation("Login Entra ID concluído para UsuarioId={UsuarioId}", usuario.Id);

            return Redirect($"{FrontendLoginUrl}?ssoOk=1");
        }
    }
}
