using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public sealed class AgendamentoAdminController : ControllerBase
    {
        private readonly IAgendamentoAdminService _service;

        public AgendamentoAdminController(IAgendamentoAdminService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddAgendamento(
            [FromBody] AgendamentoAdminCreateDto dto,
            CancellationToken token)
        {
            var agendamentoCriado = await _service.CreateAvulso(dto, token);
            return Ok(agendamentoCriado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            var agendamento = await _service.GetById(id, token);
            return Ok(agendamento);
        }

        [HttpGet]
        public async Task<IActionResult> GetByFilters
            (
                [FromQuery] AgendamentoFilterDto filtros,
                CancellationToken cancellationToken
            )
        {
            var result = await _service.GetByFiltros(filtros, cancellationToken);
            return Ok(result);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAgendamento(
            [FromRoute] Guid id,
            [FromBody] AgendamentoAdminUpdateDto dto
            )
        {
            var agendamento = await _service.Update(id, dto);

            if (agendamento == null)
            {
                return NotFound();
            }

            return Ok(agendamento);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAgendamento(
          [FromRoute] Guid id,
          CancellationToken token = default
          )
        {
            await _service.Delete(id, token);
            return NoContent();
        }
    }
}
