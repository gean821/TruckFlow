using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;

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
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20,
            CancellationToken token = default)
        {
            var items = await _service.ListarMinhasAsync(skip, take, token);
            return Ok(items);
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