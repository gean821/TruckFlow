using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Dto.Shared;
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
        private readonly IRelatorioBiService _biService;

        public RelatorioController(IRelatorioService service, IRelatorioBiService biService)
        {
            _service = service;
            _biService = biService;
        }

        [HttpGet("visao-geral")]
        [ProducesResponseType(typeof(VisaoGeralResponseDto), 200)]
        public async Task<IActionResult> VisaoGeral(
            [FromQuery] RelatorioBiFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.VisaoGeralAsync(filtro, token));
        }

        [HttpGet("recebimentos")]
        [ProducesResponseType(typeof(RecebimentosResponseDto), 200)]
        public async Task<IActionResult> Recebimentos(
            [FromQuery] RecebimentosFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.RecebimentosAsync(filtro, token));
        }

        [HttpGet("capacidade-docas")]
        [ProducesResponseType(typeof(CapacidadeDocasResponseDto), 200)]
        public async Task<IActionResult> CapacidadeDocas(
            [FromQuery] RelatorioBiFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.CapacidadeDocasAsync(filtro, token));
        }

        [HttpGet("produtividade")]
        [ProducesResponseType(typeof(ProdutividadeResponseDto), 200)]
        public async Task<IActionResult> Produtividade(
            [FromQuery] ProdutividadeFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.ProdutividadeAsync(filtro, token));
        }

        [HttpGet("produtividade/patio")]
        [ProducesResponseType(typeof(ProdutividadePatioDto), 200)]
        public async Task<IActionResult> ProdutividadePatio(
            [FromQuery] ProdutividadePatioFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.PatioAsync(filtro, token));
        }

        [HttpGet("fornecedores")]
        [ProducesResponseType(typeof(FornecedoresResponseDto), 200)]
        public async Task<IActionResult> Fornecedores(
            [FromQuery] FornecedoresFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.FornecedoresAsync(filtro, token));
        }

        [HttpGet("fornecedores/detalhe")]
        [ProducesResponseType(typeof(FornecedorDetalheDto), 200)]
        public async Task<IActionResult> FornecedorDetalhe(
            [FromQuery] FornecedoresFiltroDto filtro,
            CancellationToken token)
        {
            return Ok(await _biService.FornecedorDetalheAsync(filtro, token));
        }

        [HttpGet("agendamentos/detalhado")]
        [ProducesResponseType(typeof(PagedResponse<AgendamentoDetalhadoDto>), 200)]
        public async Task<IActionResult> AgendamentosDetalhado(
            [FromQuery] RelatorioAgendamentoDetalhadoQueryDto query,
            CancellationToken token)
        {
            return Ok(await _service.ListarDetalhadoAsync(query, token));
        }

        [HttpGet("agendamentos/resumo")]
        [ProducesResponseType(typeof(RelatorioDetalhadoResumoDto), 200)]
        public async Task<IActionResult> AgendamentosResumo(
            [FromQuery] RelatorioAgendamentoFilterDto filtros,
            CancellationToken token)
        {
            return Ok(await _service.ResumoDetalhadoAsync(filtros, token));
        }

        [HttpGet("agendamentos")]
        public async Task GerarRelatorioAgendamentos(
            [FromQuery] FormatoRelatorio formato,
            [FromQuery] RelatorioAgendamentoFilterDto filtros,
            CancellationToken token)
        {
            
            var arquivo = await _service.GerarRelatorioAgendamentos(filtros, formato, token);

            Response.ContentType = arquivo.ContentType;
            Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = arquivo.NomeArquivo,
                FileNameStar = arquivo.NomeArquivo
            }.ToString();

            await arquivo.EscreverConteudo(Response.Body, token);
        }
    }
}
