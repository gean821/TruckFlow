using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/recebimentos")]
    [Authorize(Roles = RoleGroups.CanManageGrade)]
    public class RecebimentosController : ControllerBase
    {
        private readonly IRecebimentoEventoService _service;

        public RecebimentosController(IRecebimentoEventoService service) => _service = service;

        [HttpPost("registrar-entrada/{id}")]
        public async Task<IActionResult> RegistrarEntrada(
            [FromRoute] Guid id,
            [FromBody] RegistrarEntradaDto dto,
            CancellationToken token
        )
        {
            await _service.RegistrarRecebimentoManual(
                id,
                dto.Quantidade,
                dto.Observacao,
                token
            );

            return NoContent();
        }

        [HttpGet("orfaos")]
        public async Task<IActionResult> GetOrfaos(
            [FromQuery] RecebimentoOrfaoQueryDto query,
            CancellationToken token)
        {
            var orfaos = await _service.GetOrfaos(query, token);
            return Ok(orfaos);
        }

        [HttpPost("orfaos/{id}/vincular")]
        public async Task<IActionResult> VincularOrfao(
            [FromRoute] Guid id,
            [FromBody] VincularOrfaoDto dto,
            CancellationToken token
        )
        {
            await _service.VincularOrfao(id, dto.ItemPlanejamentoId, token);
            return NoContent();
        }
    }
}
