using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthMotoristaController: ControllerBase
    {
        private readonly IUsuarioService _service;

        public AuthMotoristaController(IUsuarioService service)
        {
            _service = service;
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
            var usuarioLogado = await _service.LoginMotoristaAsync(dto, token);
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
    }
}
