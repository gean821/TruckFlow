using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Dto.Agendamento;
using TruckFlow.Application.Interfaces;
namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public sealed class AgendamentoController : ControllerBase
    {
        private readonly IAgendamentoService _service;

        public AgendamentoController(IAgendamentoService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddAgendamento(
            [FromBody] AgendamentoCreateDto dto,
            CancellationToken token)
        {
            var agendamentoCriado = await _service.AddAgendamento(dto, token);
            return Ok(agendamentoCriado);
        }

        [HttpGet("id")]

        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            var agendamento = await _service.GetById(id, token);
            
            if (agendamento == null)
            {
                return NotFound();
            }

            return Ok(agendamento);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken token)
        {
            var agendamentos = await _service.GetAll(token);
            return Ok(agendamentos);
        }

        [HttpPut("id")]
        public async Task<IActionResult> UpdateAgendamento(
            [FromRoute] Guid id,
            [FromBody] AgendamentoUpdateDto dto
            )
        {
            var agendamento = await _service.Update(id, dto);
            
            if (agendamento == null)
            {
                return NotFound();
            }

            return Ok(agendamento);
        }
    }
}
