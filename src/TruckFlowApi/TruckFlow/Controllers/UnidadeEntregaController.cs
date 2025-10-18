using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Dto.UnidadeEntrega;
using TruckFlow.Application.Interfaces;
namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class UnidadeEntregaController : ControllerBase
    {
        private readonly IUnidadeEntregaService _service;

        public UnidadeEntregaController(IUnidadeEntregaService service) => 
            _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateUnidadeEntrega(
            [FromBody] UnidadeEntregaCreateDto unidade, CancellationToken ct)
        {
            var unidadeCriada = await _service.CreateUnidadeEntrega(unidade, ct);
            return Ok(unidadeCriada);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var unidade = await _service.GetById(id, ct);

            if (unidade == null)
            {
                return NotFound();
            }

            return Ok(unidade);
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
            [FromBody] UnidadeEntregaUpdateDto dto,
            CancellationToken ct)
        {
            var unidadeEditada = await _service.UpdateUnidadeEntrega(id, dto, ct);
            return Ok(unidadeEditada);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            await _service.DeleteUnidadeEntrega(id, ct);
            return NoContent();
        }
    }
}
