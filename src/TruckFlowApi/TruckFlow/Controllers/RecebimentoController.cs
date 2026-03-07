using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Recebimento;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/recebimentos")]
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
    }
}
