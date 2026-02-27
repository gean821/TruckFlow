using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Empresa;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class EmpresaController(IEmpresaService empresaService) : ControllerBase
    {
        private readonly IEmpresaService _empresaService = empresaService;

        [HttpPost]
        public async Task<IActionResult> Create(
       [FromBody] EmpresaCreateDto dto,
       CancellationToken token)
        {
            var empresa = await _empresaService.CreateEmpresa(dto, token);

            return CreatedAtAction(
                nameof(GetById),
                new { id = empresa.Id },
                empresa);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            CancellationToken token)
        {
            var empresas = await _empresaService.GetAll(token);
            return Ok(empresas);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            var empresa = await _empresaService.GetById(id, token);
            return Ok(empresa);
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] EmpresaUpdateDto dto,
            CancellationToken token)
        {
            var empresa = await _empresaService.Update(id, dto, token);
            return Ok(empresa);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken token)
        {
            await _empresaService.Desativar(id, token);
            return NoContent();
        }
    }
}
