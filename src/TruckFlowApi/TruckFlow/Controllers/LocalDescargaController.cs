using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Dto.LocalDescarga;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class LocalDescargaController : ControllerBase
    {
        private readonly ILocalDescargaService _service;

        public LocalDescargaController(ILocalDescargaService service) =>
            _service = service;


        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] LocalDescargaCreateDto dto,
            CancellationToken token)
        {
            var localDescarga = await _service.CreateLocalDescarga(dto, token);
            return Ok(localDescarga);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var lista = await _service.GetAll();
            return Ok(lista);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken token)
        {
            var localEncontrado = await _service.GetById(id, token);
            
            if (localEncontrado == null)
            {
                return NotFound();
            }

            return Ok(localEncontrado);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            LocalDescargaUpdateDto dto,
            CancellationToken token)
        {
            var localAtualizado = await _service.Update(id, dto,token);
            return Ok(localAtualizado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            await _service.Delete(id,token);
            return NoContent();
        }
    }
}
