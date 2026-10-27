using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    [Authorize(Roles = RoleGroups.CanViewSchedule)]
    public class DashboardController: ControllerBase
    {
        private readonly IDashboardService _service;
        private readonly IRelatorioBiService _biService;

        public DashboardController(IDashboardService service, IRelatorioBiService biService)
        {
            _service = service;
            _biService = biService;
        }

        [HttpGet("hoje")]
        public async Task<IActionResult> GetHoje([FromQuery] Guid? unidadeEntregaId, CancellationToken token)
        {
            return Ok(await _biService.DashboardHojeAsync(unidadeEntregaId, token));
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken token)
        {
            var summary = await _service.GetDashboardSummaryAsync(token);
            return Ok(summary);
        }
    }
}

