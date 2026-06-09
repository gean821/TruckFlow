using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Dto.Auth;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthMotoristaController: ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly IVerificacaoEmailService _verificacaoService;

        public AuthMotoristaController(IUsuarioService service, IVerificacaoEmailService verificacaoService)
        {
            _service = service;
            _verificacaoService = verificacaoService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register
            (
                [FromBody] UserMotoristaRegisterDto dto,
                CancellationToken token = default
            )
        {
            var usuario = await _service.RegisterMotoristaAsync(dto, token);
            return Ok(usuario);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login
            (
                [FromBody] UserMotoristaLoginDto dto,
                CancellationToken token = default
            )
        {
            var usuarioLogado = await _service.LoginMotoristaAsync(
                dto,
                Request.Headers.UserAgent.ToString(),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                token);
            return Ok(usuarioLogado);
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMotorista
            (
                [FromBody] UserMotoristaUpdateDto dto,
                CancellationToken token = default
            )
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var usuarioAtualizado = await _service.UpdateMotoristaAsync(usuarioId, dto, token);
            
            return Ok(usuarioAtualizado);
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpDelete("me")]
        public async Task<IActionResult> Delete
           (
               CancellationToken token = default
           )
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            await _service.DeleteMotoristaAsync(usuarioId, token);
            
            return NoContent();
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpPost("enviar-codigo")]
        public async Task<IActionResult> EnviarCodigo(
            [FromBody] EnviarCodigoEmailDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            await _verificacaoService.EnviarCodigoAsync(usuarioId, dto.Finalidade, token);
            return Ok(new { message = "Código enviado para o seu e-mail." });
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpPost("verificar-codigo")]
        public async Task<IActionResult> VerificarCodigo(
            [FromBody] VerificarCodigoEmailDto dto,
            CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var codigoToken = await _verificacaoService.ValidarCodigoAsync(usuarioId, dto.Codigo, dto.Finalidade, token);
            return Ok(new { codigoToken });
        }

        [Authorize(Roles = Roles.Motorista)]
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

        [Authorize(Roles = Roles.Motorista)]
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
    }
}
