using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Conferencia;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Controllers
{
    [ApiController]
    [Route("v1/[Controller]")]
    [Authorize(Roles = RoleGroups.CanCheckIn)]
    public sealed class ConferenciaController : ControllerBase
    {
        private readonly IConferenciaService _service;

        public ConferenciaController(IConferenciaService service)
        {
            _service = service;
        }

        /// <summary>
        /// Lista os itens da nota fiscal do agendamento com status de match e sugestões
        /// pra cada item pendente. Usado pela tela de conferência na portaria.
        /// </summary>
        [HttpGet("agendamento/{agendamentoId:guid}")]
        [ProducesResponseType(typeof(ConferenciaResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByAgendamento(
            [FromRoute] Guid agendamentoId,
            CancellationToken token)
        {
            var conferencia = await _service.GetByAgendamentoIdAsync(agendamentoId, token);
            return Ok(conferencia);
        }

        /// <summary>
        /// Admin escolhe o produto pra um item pendente. Sistema atualiza item e
        /// aprende em ProdutoFornecedor — próxima NF do mesmo fornecedor com mesmo cProd
        /// matcha automaticamente.
        /// </summary>
        [HttpPost("item/{itemId:guid}/match")]
        [ProducesResponseType(typeof(ConferenciaItemDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> MatchItem(
            [FromRoute] Guid itemId,
            [FromBody] MatchItemDto dto,
            CancellationToken token)
        {
            var atualizado = await _service.MatchItemAsync(itemId, dto.ProdutoId, token);
            return Ok(atualizado);
        }
    }
}