using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class AuthAdminController : ControllerBase
    {
        private readonly IUsuarioService _service;

        public AuthAdminController(IUsuarioService service)
        {
            _service = service;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register
            (
                [FromBody] UserAdminRegisterDto dto,
                CancellationToken token = default
            )
        {
            var usuario = await _service.RegisterAdminAsync(dto, token);
            return Ok(usuario);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login
            (
                [FromBody] UserAdminLoginDto dto,
                CancellationToken token = default
            )
        {
            var usuarioLogado = await _service.LoginAdminAsync(dto, token);
            return Ok(usuarioLogado);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin
            (
                [FromRoute] Guid id,
                [FromBody] UserAdminEditDto dto,
                CancellationToken token = default
            )
        {
            var usuarioAtualizado = await _service.UpdateAdminAsync(id, dto, token);
            return Ok(usuarioAtualizado);
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete
           (
               [FromRoute] Guid id,
               CancellationToken token = default
           )
        {
            await _service.DeleteAdminAsync(id, token);
            return NoContent();
        }
    }
}
