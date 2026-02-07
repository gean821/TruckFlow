using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public class DashboardController: ControllerBase
    {
        private readonly IDashboardService _service;

        public DashboardController(IDashboardService service) => _service = service;

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(CancellationToken token)
        {
            var summary = await _service.GetDashboardSummaryAsync(token);
            return Ok(summary);
        }
    }
}

