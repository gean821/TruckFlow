using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;
using TruckFlow.Extensions.Auth;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthAdminController : ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly IWebHostEnvironment _env;

        public AuthAdminController(IUsuarioService service, IWebHostEnvironment env)
        {
            _service = service;
            _env = env;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] UserAdminRegisterDto dto,
            CancellationToken token = default)
        {
            var usuario = await _service.RegisterAdminAsync(dto, token);
            return Ok(usuario);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] UserAdminLoginDto dto,
            CancellationToken token = default)
        {
            var usuarioLogado = await _service.LoginAdminAsync(
                dto,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                token);

            RefreshCookieHelper.SetRefresh(
                Response,
                _env,
                usuarioLogado.RefreshToken,
                usuarioLogado.RefreshTokenExpiresAt);

            return Ok(new
            {
                usuarioLogado.Token,
                usuarioLogado.TokenExpiresAt,
                usuarioLogado.Usuario
            });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateAdmin(
            [FromRoute] Guid id,
            [FromBody] UserAdminEditDto dto,
            CancellationToken token = default)
        {
            var usuarioAtualizado = await _service.UpdateAdminAsync(id, dto, token);
            return Ok(usuarioAtualizado);
        }
    }
}