using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/internal/notifications")]
    [Authorize(Roles = Roles.Admin)]
    public sealed class NotificationsInternalController : ControllerBase
    {
        private readonly INotificacaoStatsService _stats;

        public NotificationsInternalController(INotificacaoStatsService stats)
        {
            _stats = stats;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats(CancellationToken token = default)
        {
            var result = await _stats.GetAsync(token);
            return Ok(result);
        }
    }
}