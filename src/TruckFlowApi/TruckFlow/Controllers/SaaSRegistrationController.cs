using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Extensions.Auth;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/registro")]
    public class RegistroController : ControllerBase
    {
        private readonly ISaaSRegistrationService _service;
        private readonly IWebHostEnvironment _env;

        public RegistroController(
            ISaaSRegistrationService service,
            IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(
            [FromBody] RegisterEmpresaAdminDto dto,
            CancellationToken token)
        {
            var response = await _service.RegisterAsync(
                dto,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                token);

            RefreshCookieHelper.SetRefresh(
                Response,
                _env,
                response.RefreshToken,
                response.RefreshTokenExpiresAt);

            return Ok(new
            {
                response.Token,
                response.TokenExpiresAt,
                response.Usuario
            });
        }
    }
}