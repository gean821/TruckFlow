using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Empresa;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/registro")]
    public class RegistroController : ControllerBase
    {
        private readonly ISaaSRegistrationService _service;

        public RegistroController(ISaaSRegistrationService service)
        {
            _service = service;
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register(
            [FromBody] RegisterEmpresaAdminDto dto,
            CancellationToken token)
        {
            var response = await _service.RegisterAsync(dto, token);
            return Ok(response);
        }
    }
}
