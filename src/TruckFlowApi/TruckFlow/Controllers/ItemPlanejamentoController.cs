using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class ItemPlanejamentoController : ControllerBase
    {
        private readonly IItemPlanejamentoService _service;

        public ItemPlanejamentoController(IItemPlanejamentoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem
            (
             [FromBody] ItemPlanejamentoCreateDto dto,
             CancellationToken token = default
            )
        {
            var item = await _service.CreateItem(dto, token);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken token = default)
        {
            var itens = await _service.GetAll(token);
            return Ok(itens);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById
            (
             [FromRoute] Guid id,
             CancellationToken token = default
            )
        {
            var item = await _service.GetById(id, token);
            if (item == null)
            {
                return NotFound();
            }

            return Ok(item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update
            (
             [FromRoute] Guid id,
             ItemPlanejamentoUpdateDto dto,
             CancellationToken token = default
            )
        {
            var item = await _service.UpdateItem(id, dto, token);
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete
            (
            [FromRoute] Guid id,
            CancellationToken token = default
            )
        {
            await _service.DeleteItem(id, token);
            return NoContent();
        }
    }
}
