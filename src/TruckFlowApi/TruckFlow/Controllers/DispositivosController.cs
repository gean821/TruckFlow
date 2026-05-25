using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/dispositivos")]
    [Authorize]
    public sealed class DispositivosController : ControllerBase
    {
        private readonly IDispositivoUsuarioService _service;

        public DispositivosController(IDispositivoUsuarioService service)
        {
            _service = service;
        }

        [HttpPost("registrar")]
        public async Task<IActionResult> Registrar(
            [FromBody] RegistrarDispositivoDto dto,
            CancellationToken token = default)
        {
            await _service.RegistrarAsync(dto, token);
            return NoContent();
        }
    }
}