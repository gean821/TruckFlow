using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Audit;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/audit")]
    [Authorize(Roles = RoleGroups.CanViewAuditLogs)]
    public class AuditLogController(IAuditLogService service) : ControllerBase
    {
        private readonly IAuditLogService _service = service;

        [HttpGet]
        public async Task<IActionResult> GetPaged(
            [FromQuery] AuditLogListQueryDto query,
            CancellationToken token = default)
        {
            var result = await _service.GetPaged(query, token);
            return Ok(result);
        }
    }
}
