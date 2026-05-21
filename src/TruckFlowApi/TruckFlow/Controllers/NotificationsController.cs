using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/notifications")]
    [Authorize]
    public sealed class NotificationsController : ControllerBase
    {
        private readonly INotificacaoService _service;

        public NotificationsController(INotificacaoService service)
        {
            _service = service;
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
    }
}