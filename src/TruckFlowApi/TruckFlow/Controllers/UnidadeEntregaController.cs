using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.UnidadeEntrega;
using TruckFlow.Domain.Entities;
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
            return CreatedAtAction(
                 nameof(GetById),
                 new { id = unidadeCriada.Id },
                 unidadeCriada);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var unidade = await _service.GetById(id, ct);
            return Ok(unidade);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var lista = await _service.GetAll(ct);
            return Ok(lista);
        }

        [HttpPatch("{id}")]
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


        [HttpPatch("{id}/status")]
        public async Task<IActionResult> MudarStatus(
            [FromRoute] Guid id,
            [FromBody] UpdateStatusDto dto,
            CancellationToken token = default)
        {
            var status = await _service.MudarStatusUnidade(
                id,
                dto.StatusUnidade,
                token
            );

            return Ok(status);
        }
    }
}
