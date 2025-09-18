using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Dto.Veiculo;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class VeiculoController : ControllerBase
    {
        private readonly IVeiculoService _service;

        public VeiculoController(IVeiculoService service) =>
            _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateVeiculo(
            [FromBody] VeiculoCreateDto veiculo, CancellationToken ct)
        {
            var veiculoCriado = await _service.CreateVeiculo(veiculo, ct);
            return Ok(veiculoCriado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var veiculo = await _service.GetById(id, ct);

            if (veiculo == null)
            {
                return NotFound();
            }

            return Ok(veiculo);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var lista = await _service.GetAll(ct);
            return Ok(lista);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] VeiculoUpdateDto dto,
            CancellationToken ct)
        {
            var veiculoEditado = await _service.UpdateVeiculo(id, dto, ct);
            return Ok(veiculoEditado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            await _service.DeleteVeiculo(id, ct);
            return NoContent();
        }
    }
}
