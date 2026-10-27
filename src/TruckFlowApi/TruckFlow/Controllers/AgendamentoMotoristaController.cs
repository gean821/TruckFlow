using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.RelatorioMotorista;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    [Authorize(Roles = "Motorista")]
    public class AgendamentoMotoristaController : ControllerBase
    {
        private readonly IAgendamentoMotoristaService _service;
        private readonly IRelatorioMotoristaService _relatorioService;

        public AgendamentoMotoristaController(
            IAgendamentoMotoristaService service,
            IRelatorioMotoristaService relatorioService)
        {
            _service = service;
            _relatorioService = relatorioService;
        }

        /// <summary>
        /// Lista os horários disponíveis (vagas) compatíveis com os produtos da nota fiscal informada.
        /// O backend resolve a nota pela chave de acesso (já persistida via /parse + /save) e cruza
        /// os produtos dos itens com as vagas abertas. Vagas restritas a um fornecedor específico
        /// só aparecem se o fornecedor da nota bater.
        /// </summary>
        /// <param name="chaveAcesso">Chave de acesso da NF-e (44 dígitos), já salva no sistema</param>
        /// <param name="data">Data desejada (Ex: 2025-01-20)</param>
        [HttpGet("disponiveis")]
        [ProducesResponseType(typeof(List<AgendamentoMotoristaResponse>), 200)]
        public async Task<IActionResult> GetAvailableAppointments(
            [FromQuery] string chaveAcesso,
            [FromQuery] DateTime data,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            {
                return BadRequest("Chave de acesso inválida.");
            }

            var vagas = await _service.GetAvailableAppointments(chaveAcesso, data, token);
            return Ok(vagas);
        }

        /// <summary>
        /// Lista o histórico e agendamentos futuros do motorista logado.
        /// O motorista é identificado pelo claim "MotoristaId" do JWT.
        /// </summary>
        [HttpGet("meus-agendamentos")]
        [ProducesResponseType(typeof(List<AgendamentoMotoristaResponse>), 200)]
        public async Task<IActionResult> GetDriverAppointments(CancellationToken token = default)
        {
            var agendamentos = await _service.GetDriverAppointments(token);
            return Ok(agendamentos);
        }

        [HttpGet("relatorio")]
        [ProducesResponseType(typeof(RelatorioMotoristaResponseDto), 200)]
        public async Task<IActionResult> GetRelatorio(
            [FromQuery] RelatorioMotoristaQueryDto query,
            CancellationToken token = default)
        {
            var relatorio = await _relatorioService.GerarAsync(query, token);
            return Ok(relatorio);
        }


        /// <summary>
        /// Reserva uma vaga (Slot) vinculando a Nota Fiscal do motorista.
        /// </summary>
        [HttpPost("reservar")]
        [ProducesResponseType(typeof(AgendamentoMotoristaResponse), 200)]
        [ProducesResponseType(400)] 
        [ProducesResponseType(404)]
        public async Task<IActionResult> ReservarVaga(
            [FromBody] ReservarAgendamentoDto dto,
            CancellationToken token = default)
        {
            var resultado = await _service.BookAppointment(dto,token);
            return Ok(resultado);
        }
    }
}
