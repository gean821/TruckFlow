using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;
using TruckFlow.Extensions.Auth;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthAdminController : ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly IWebHostEnvironment _env;
        private readonly IVerificacaoEmailService _verificacaoService;

        public AuthAdminController(IUsuarioService service, IWebHostEnvironment env, IVerificacaoEmailService verificacaoService)
        {
            _service = service;
            _env = env;
            _verificacaoService = verificacaoService;
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

        [Authorize]
        [HttpPost("enviar-codigo")]
        public async Task<IActionResult> EnviarCodigo(
            [FromBody] EnviarCodigoEmailDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            await _verificacaoService.EnviarCodigoAsync(usuarioId, dto.Finalidade, token);
            return Ok(new { message = "Código enviado para o seu e-mail." });
        }

        [Authorize]
        [HttpPost("verificar-codigo")]
        public async Task<IActionResult> VerificarCodigo(
            [FromBody] VerificarCodigoEmailDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var codigoToken = await _verificacaoService.ValidarCodigoAsync(usuarioId, dto.Codigo, dto.Finalidade, token);
            return Ok(new { codigoToken });
        }

        [Authorize]
        [HttpPost("alterar-senha")]
        public async Task<IActionResult> AlterarSenha(
            [FromBody] AlterarSenhaComCodigoDto dto,
            CancellationToken token = default)
        {
            if (dto.NovaSenha != dto.ConfirmarSenha)
                return BadRequest(new { message = "As senhas não coincidem." });

            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var payload = _verificacaoService.ExtrairCodigoToken(dto.CodigoToken);

            if (payload.UsuarioId != usuarioId || payload.Finalidade != FinalidadeVerificacaoEmail.AlterarSenha)
                return BadRequest(new { message = "Token de verificação inválido." });

            await _service.AlterarSenhaComCodigoAsync(usuarioId, dto.NovaSenha, token);
            return Ok(new { message = "Senha alterada com sucesso." });
        }

        [Authorize]
        [HttpPost("alterar-email")]
        public async Task<IActionResult> AlterarEmail(
            [FromBody] AlterarEmailComCodigoDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var payload = _verificacaoService.ExtrairCodigoToken(dto.CodigoToken);

            if (payload.UsuarioId != usuarioId || payload.Finalidade != FinalidadeVerificacaoEmail.AlterarEmail)
                return BadRequest(new { message = "Token de verificação inválido." });

            await _service.AlterarEmailComCodigoAsync(usuarioId, dto.NovoEmail, token);
            return Ok(new { message = "E-mail alterado com sucesso." });
        }

        [Authorize]
        [HttpPatch("me")]
        public async Task<IActionResult> AtualizarPerfil(
            [FromBody] AtualizarPerfilAdminDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            await _service.AtualizarPerfilAsync(usuarioId, dto, token);
            return Ok(new { message = "Perfil atualizado com sucesso." });
        }
    }
}