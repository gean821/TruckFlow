using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/agendamento-motorista")]
    public class AgendamentoMotoristaController : ControllerBase
    {
        private readonly IAgendamentoMotoristaService _service;

        public AgendamentoMotoristaController(IAgendamentoMotoristaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista os horários disponíveis (vagas) para um determinado fornecedor e dia.
        /// </summary>
        /// <param name="fornecedorId">ID do Fornecedor que o motorista vai carregar</param>
        /// <param name="data">Data desejada (Ex: 2025-01-20)</param>
        [HttpGet("disponiveis")]
        [ProducesResponseType(typeof(List<AgendamentoMotoristaResponse>), 200)]
        public async Task<IActionResult> GetAvailableAppointments(
            [FromQuery] Guid fornecedorId,
            [FromQuery] DateTime data,
            CancellationToken token)
        {
            var vagas = await _service.GetAvailableAppointments(fornecedorId, data, token);
            return Ok(vagas);
        }

        /// <summary>
        /// Lista o histórico e agendamentos futuros do motorista logado.
        /// </summary>
        [HttpGet("meus-agendamentos/{motoristaId}")]
        [ProducesResponseType(typeof(List<AgendamentoMotoristaResponse>), 200)]
        public async Task<IActionResult> GetDriverAppointments
            (
                [FromRoute] Guid motoristaId,
                CancellationToken token = default
            )
        {
            var agendamentos = await _service.GetDriverAppointments(motoristaId, token);
            return Ok(agendamentos);
        }


        /// <summary>
        /// Reserva uma vaga (Slot) vinculando a Nota Fiscal do motorista.
        /// </summary>
        [HttpPost("reservar")]
        [ProducesResponseType(typeof(AgendamentoMotoristaResponse), 200)]
        [ProducesResponseType(400)] // Erro de negócio (vaga ocupada, etc)
        [ProducesResponseType(404)] // Vaga não encontrada
        public async Task<IActionResult> ReservarVaga(
            [FromBody] ReservarAgendamentoDto dto,
            CancellationToken token = default)
        {
           
            var resultado = await _service.BookAppointment(dto.AgendamentoId, dto.NotaFiscalChaveAcesso, dto.UsuarioId, token);
            return Ok(resultado);
        }
    }
}
