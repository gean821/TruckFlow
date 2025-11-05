using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("/v1/[Controller]")]
    public class NotaFiscalController : ControllerBase
    {
        private readonly INotaFiscalService _service;

        public NotaFiscalController(INotaFiscalService service) =>
            _service = service;

        [HttpPost("parse")]
        public async Task<IActionResult> ParseNotaFiscalXml(
            IFormFile xmlFile,
            CancellationToken token)
        {
            if (xmlFile == null || xmlFile.Length == 0)
            {
                return BadRequest("Nenhum arquivo Xml enviado.");
            }

            using var stream = xmlFile.OpenReadStream();
            var notaFiscalDto = await _service.ParseXmlAsync(stream, token);
            return Ok(notaFiscalDto);
        }

        [HttpPost("save")]
        public async Task<IActionResult> SaveParsedData
            (
                [FromBody] NotaFiscalParsedDto dto,
                CancellationToken token
            )
        {
            if (dto == null)
            {
                return BadRequest("os dados da nota Fiscal são obrigátorios.");
            }

            var userId = Guid.NewGuid(); //ajustar para pegar do JWT, por hora funciona só para testes..

            var nota = await _service.SaveParsedNotaAsync(dto, userId,token);

            return CreatedAtAction(nameof(BuscarNotaPorChave),
                                   new { chaveAcesso = nota.ChaveAcesso },
                                   nota);
        }

        [HttpGet("buscar-por-chave/{chaveAcesso}")]
        public async Task<IActionResult> BuscarNotaPorChave(string chaveAcesso, CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(chaveAcesso) || chaveAcesso.Length != 44)
            {
                return BadRequest("Chave de acesso inválida.");
            }

            var notaFiscalDto = await _service.ObterPorChaveAsync(chaveAcesso, token);

            if (notaFiscalDto == null)
            {
                return NotFound("Nenhuma nota encontrada com essa chave de acesso.");
            }

            return Ok(notaFiscalDto);
        }
    }
}
