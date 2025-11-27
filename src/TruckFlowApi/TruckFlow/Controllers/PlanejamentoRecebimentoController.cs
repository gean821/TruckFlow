using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Recebimento;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("/v1/[Controller]")]
    public class PlanejamentoRecebimentoController : ControllerBase
    {
        private readonly IPlanejamentoRecebimentoService _service;

        public PlanejamentoRecebimentoController
            (IPlanejamentoRecebimentoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRecebimento
            (
                [FromBody] RecebimentoCreateDto recebimento,
                CancellationToken token = default
            )
        {
            var recebimentoCriado = await _service.CreateRecebimento(recebimento, token);
            return Ok(recebimentoCriado);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken token = default)
        {
            var recebimentos = await _service.GetAll(token);
            return Ok(recebimentos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAll
            (
                [FromRoute] Guid id,
                CancellationToken token = default
            )
        {
            var recebimento = await _service.GetById(id, token);
            
            if (recebimento == null)
            {
                return NotFound();
            }

            return Ok(recebimento);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecebimento
            (
                [FromRoute] Guid id,
                [FromBody] RecebimentoUpdateDto dto,
                CancellationToken token = default
            )
        {
            var recebimento = await _service.UpdateRecebimento(id, dto, token);
            return Ok(recebimento);
        }

        [HttpDelete("{id}")]   
        public async Task<IActionResult> Delete
            (
                [FromRoute] Guid id,
                CancellationToken token = default
            )
        {
            await _service.DeleteRecebimento(id, token);
            return NoContent();
        }
    }
}
