using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.LocalDescarga;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class LocalDescargaController(ILocalDescargaService service) : ControllerBase
    {
        private readonly ILocalDescargaService _service = service;

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] LocalDescargaCreateDto dto,
            CancellationToken token)
        {
            var localDescarga = await _service.CreateLocalDescarga(dto, token);

            return CreatedAtAction(
                nameof(GetById),
                new { id = localDescarga.Id },
                localDescarga);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken token)
        {
            var lista = await _service.GetAll(token);
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            var localEncontrado = await _service.GetById(id, token);
            return Ok(localEncontrado);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] LocalDescargaUpdateDto dto,
            CancellationToken token)
        {
            var localAtualizado = await _service.Update(id, dto, token);
            return Ok(localAtualizado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            await _service.Delete(id, token);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> MudarStatus
            (
                [FromRoute] Guid id,
                [FromBody] MudarStatusLocal dto,
                CancellationToken token = default
            )
        {
            var local =  await _service.MudarStatusLocal(
                id,
                dto.Status,
                token
            );

            return Ok(local);
        }
    }
}