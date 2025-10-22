using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]

    public class ProdutoController : ControllerBase
    {
        private readonly IProdutoService _service;

        public ProdutoController(IProdutoService service) =>
            _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateProduto(
            [FromBody] ProdutoCreateDto produto,
            CancellationToken ct)
        {
            var produtoCriado = await _service.CreateProduto(produto, ct);
            return Ok(produtoCriado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var produto = await _service.GetById(id, ct);
            
            if (produto == null)
            {
                return NotFound();
            }

            return Ok(produto);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var lista = await _service.GetAll(ct);
            return Ok(lista);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            [FromRoute] Guid id,
            [FromBody] ProdutoEditDto dto,
            CancellationToken ct)
        {
            var produtoEditado = await _service.UpdateProduto(id, dto, ct);
            return Ok(produtoEditado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            await _service.DeleteProduto(id,ct);
            return NoContent();
        }
    }
}
