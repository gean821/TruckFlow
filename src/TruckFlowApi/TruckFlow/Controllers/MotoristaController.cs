using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [Route("v1/[Controller]")]
    [ApiController]
    public class MotoristaController: ControllerBase
    {
        private readonly IMotoristaService _service;

        public MotoristaController(IMotoristaService service)
        {
            _service = service;
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpGet("veiculos")]
        public async Task<IActionResult> GetMeusVeiculos(CancellationToken token)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);

            var result = await _service.GetMeusVeiculos(usuarioId, token);
            return Ok(result);
        }

        [Authorize(Roles = Roles.Motorista)]
        [HttpGet()]
        public async Task<IActionResult> GetMe(CancellationToken token = default)
        {
            var usuarioId = Guid.Parse(User.FindFirst("UserId")!.Value);
            var result = await _service.GetMe(usuarioId, token);
            return Ok(result);
        }
    }
}
