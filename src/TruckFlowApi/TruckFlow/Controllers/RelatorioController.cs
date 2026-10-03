using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    [Authorize(Roles = RoleGroups.CanViewSchedule)]
    public sealed class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _service;

        public RelatorioController(IRelatorioService service)
        {
            _service = service;
        }

        [HttpGet("agendamentos")]
        public async Task<IActionResult> GerarRelatorioAgendamentos(
            [FromQuery] FormatoRelatorio formato,
            [FromQuery] RelatorioAgendamentoFilterDto filtros,
            CancellationToken token)
        {
            var arquivo = await _service.GerarRelatorioAgendamentos(filtros, formato, token);
            return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeArquivo);
        }
    }
}
