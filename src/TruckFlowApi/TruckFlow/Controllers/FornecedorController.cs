using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    public class FornecedorController : ControllerBase
    {
        private readonly IFornecedorService _service;

        public FornecedorController(IFornecedorService service) =>
            _service = service;

        [HttpPost]
        public async Task<IActionResult> CreateFornecedor(
            [FromBody] FornecedorCreateDto fornecedor,
            CancellationToken ct)
        {
            var fornecedorCriado = await _service.CreateFornecedor(fornecedor, ct);
            return Ok(fornecedorCriado);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            var fornecedor = await _service.GetById(id, ct);

            if (fornecedor == null)
            {
                return NotFound();
            }

            return Ok(fornecedor);
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
            [FromBody] FornecedorUpdateDto dto,
            CancellationToken ct)
        {
            var fornecedorEncontrado = await _service.UpdateFornecedor(id, dto, ct);
            return Ok(fornecedorEncontrado);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(
            [FromRoute] Guid id,
            CancellationToken ct)
        {
            await _service.DeleteFornecedor(id, ct);
            return NoContent();
        }

        [HttpPost("{fornecedorId}/produtos/{produtoId}")]
        public async Task<IActionResult> AddProdutoToFornecedor
            (
                [FromRoute]Guid fornecedorId, 
                [FromRoute] Guid produtoId,
                CancellationToken token
           )
        {
            var produtoAdicionado = await _service
                .AddProdutoToFornecedorAsync(fornecedorId, produtoId, token);

            return Ok(produtoAdicionado);
        }

        [HttpDelete("{fornecedorId}/produtos/{produtoId}")]
        public async Task<IActionResult> DeleteProdutoFromFornecedor
            (
               [FromRoute] Guid fornecedorId,
               [FromRoute] Guid produtoId,
               CancellationToken token
            )
        {

            await _service.DeleteProdutoFromFornecedorAsync(fornecedorId, produtoId, token);
            return NoContent();
        }

        [HttpGet("produtos")]
        public async Task<IActionResult> GetByIdsAsyc
            (
                [FromQuery] IEnumerable<Guid> produtosIds,
                CancellationToken token
            )
        {
            var produtos = await _service.GetByIdWithProdutosAsync(produtosIds, token);
            return Ok(produtos);
        }
    }
}
