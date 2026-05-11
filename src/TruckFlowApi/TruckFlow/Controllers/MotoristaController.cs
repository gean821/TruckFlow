using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [Route("v1/[Controller]")]
    [ApiController]
    [Authorize(Roles = Roles.Motorista)]
    public class MotoristaController: ControllerBase
    {
        private readonly IMotoristaService _service;

        public MotoristaController(IMotoristaService service)
        {
            _service = service;
        }

        [HttpGet("veiculos")]
        public async Task<IActionResult> GetMeusVeiculos(CancellationToken token)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);

            var result = await _service.GetMeusVeiculos(usuarioId, token);
            return Ok(result);
        }

        [HttpGet()]
        public async Task<IActionResult> GetMe(CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var result = await _service.GetMe(usuarioId, token);
            return Ok(result);
        }
    }
}
