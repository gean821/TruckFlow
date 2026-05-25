using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Notificacao;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/notifications")]
    [Authorize]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificacaoService _service;
        private readonly INotificacaoSendService _sendService;

        public NotificationsController(
            INotificacaoService service,
            INotificacaoSendService sendService)
        {
            _service = service;
            _sendService = sendService;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] NotificacaoListQueryDto query,
            CancellationToken token = default)
        {
            var result = await _service.ListarMinhasAsync(query, token);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount(CancellationToken token = default)
        {
            var count = await _service.ContarNaoLidasMinhasAsync(token);
            return Ok(new { count });
        }

        [HttpPatch("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(
            [FromRoute] Guid id,
            CancellationToken token = default)
        {
            var found = await _service.MarcarComoLidaAsync(id, token);
            return found ? NoContent() : NotFound();
        }

        [HttpGet("agendamento/{agendamentoId:guid}")]
        public async Task<IActionResult> ListByAgendamento(
            [FromRoute] Guid agendamentoId,
            CancellationToken token = default)
        {
            var items = await _service.ListarPorAgendamentoAsync(agendamentoId, token);
            return Ok(items);
        }

        [HttpPost("send-motorista")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> SendParaMotorista(
            [FromBody] EnviarParaMotoristaDto dto,
            CancellationToken token = default)
        {
            await _sendService.EnviarParaMotoristaAsync(dto, token);
            return NoContent();
        }

        [HttpPost("send-empresa")]
        [Authorize(Roles = Roles.Motorista)]
        public async Task<IActionResult> SendParaEmpresa(
            [FromBody] EnviarParaEmpresaDto dto,
            CancellationToken token = default)
        {
            await _sendService.EnviarParaEmpresaAsync(dto, token);
            return NoContent();
        }
    }
}