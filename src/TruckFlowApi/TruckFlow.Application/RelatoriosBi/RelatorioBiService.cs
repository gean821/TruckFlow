using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Dto.RelatorioBi;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Application.RelatoriosBi
{
    public sealed class RelatorioBiService : IRelatorioBiService
    {
        private readonly IRelatorioBiRepositorio _repo;
        private readonly IEmpresaRepositorio _empresaRepo;
        private readonly CurrentUserGuard _guard;
        private readonly TimeProvider _timeProvider;
        private readonly IAgendamentoRepositorio _agendamentoRepo;

        public RelatorioBiService(
            IRelatorioBiRepositorio repo,
            IEmpresaRepositorio empresaRepo,
            CurrentUserGuard guard,
            TimeProvider timeProvider,
            IAgendamentoRepositorio agendamentoRepo)
        {
            _repo = repo;
            _empresaRepo = empresaRepo;
            _guard = guard;
            _timeProvider = timeProvider;
            _agendamentoRepo = agendamentoRepo;
        }

        public async Task<ProdutividadeResponseDto> ProdutividadeAsync(
            ProdutividadeFiltroDto filtro,
            CancellationToken token = default)
        {
            var filtroBi = filtro.ParaFiltroBi();
            var periodo = RelatorioBiPeriodo.Resolver(filtroBi, _timeProvider.GetUtcNow().UtcDateTime);
            var linhas = await _repo.ListarLinhasAsync(RelatorioBiPeriodo.Criteria(filtroBi, periodo), token);
            var fins = await FinsDaDocaAsync(linhas, token);

            return ProdutividadeCalculo.Montar(linhas, fins, periodo, ProdutividadeCalculo.ResolverLimite(filtro.LimiteEsperaMin));
        }

        public async Task<ProdutividadePatioDto> PatioAsync(
            ProdutividadePatioFiltroDto filtro,
            CancellationToken token = default)
        {
            var agoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var dia = filtro.Dia ?? RelatorioBiPeriodo.DiaLocal(agoraUtc);

            var criteria = new RelatorioBiCriteria(
                RelatorioBiPeriodo.InicioDoDiaUtc(dia),
                RelatorioBiPeriodo.InicioDoDiaUtc(dia.AddDays(1)),
                filtro.UnidadeEntregaId,
                filtro.LocalDescargaId,
                filtro.FornecedorId,
                filtro.ProdutoId);

            var linhas = await _repo.ListarLinhasAsync(criteria, token);
            var fins = await FinsDaDocaAsync(linhas, token);

            return ProdutividadeCalculo.MontarPatio(linhas, fins, dia, agoraUtc);
        }

        public async Task<FornecedoresResponseDto> FornecedoresAsync(
            FornecedoresFiltroDto filtro,
            CancellationToken token = default)
        {
            var filtroBi = filtro.ParaFiltroBi();
            var periodo = RelatorioBiPeriodo.Resolver(filtroBi, _timeProvider.GetUtcNow().UtcDateTime);
            var agrupamento = RecebimentosCalculo.ResolverAgrupamento(filtro.Agrupamento, periodo.Dias);
            var linhas = await _repo.ListarLinhasAsync(RelatorioBiPeriodo.Criteria(filtroBi, periodo), token);
            var fins = await FinsDaDocaAsync(linhas, token);

            return FornecedoresCalculo.Montar(linhas, fins, periodo, agrupamento);
        }

        public async Task<FornecedorDetalheDto> FornecedorDetalheAsync(
            FornecedoresFiltroDto filtro,
            CancellationToken token = default)
        {
            if (filtro.FornecedorId is not { } fornecedorId)
                throw new BusinessException("Informe o fornecedor.");

            var filtroBi = filtro.ParaFiltroBi(comFornecedor: false);
            var periodo = RelatorioBiPeriodo.Resolver(filtroBi, _timeProvider.GetUtcNow().UtcDateTime);
            var agrupamento = RecebimentosCalculo.ResolverAgrupamento(filtro.Agrupamento, periodo.Dias);
            var linhas = await _repo.ListarLinhasAsync(RelatorioBiPeriodo.Criteria(filtroBi, periodo), token);

            if (!linhas.Any(l => l.FornecedorId == fornecedorId))
                throw new NotFoundException("Fornecedor sem agendamentos no período.");

            var fins = await FinsDaDocaAsync(linhas, token);

            return FornecedoresCalculo.Detalhe(linhas, fins, periodo, agrupamento, fornecedorId);
        }

        private async Task<IReadOnlyDictionary<Guid, List<DateTime>>> FinsDaDocaAsync(
            IReadOnlyCollection<RelatorioBiLinha> linhas,
            CancellationToken token)
        {
            var estimaveis = linhas
                .Where(l => l.InicioDescargaEm is null && l.ChegouEm.HasValue && l.FimDescargaEm.HasValue && l.LocalDescargaId.HasValue)
                .ToList();

            if (estimaveis.Count == 0)
            {
                return Relatorios.RelatorioDetalhadoCalculo.FinsPorDoca([]);
            }

            var fins = await _agendamentoRepo.ListarFinsDescargaAsync(
                estimaveis.Min(l => l.ChegouEm!.Value),
                estimaveis.Max(l => l.FimDescargaEm!.Value),
                estimaveis.Select(l => l.LocalDescargaId!.Value).Distinct().ToList(),
                token);

            return Relatorios.RelatorioDetalhadoCalculo.FinsPorDoca(fins);
        }

        public async Task<VisaoGeralResponseDto> VisaoGeralAsync(
            RelatorioBiFiltroDto filtro,
            CancellationToken token = default)
        {
            var periodo = RelatorioBiPeriodo.Resolver(filtro, _timeProvider.GetUtcNow().UtcDateTime);
            var linhas = await _repo.ListarLinhasAsync(RelatorioBiPeriodo.Criteria(filtro, periodo), token);
            var docas = await _repo.ContarDocasAtivasAsync(filtro.UnidadeEntregaId, filtro.LocalDescargaId, token);
            var meta = await MetaSemFiltroAsync(filtro, token);

            return VisaoGeralCalculo.Montar(linhas, periodo, meta, docas);
        }

        public async Task<RecebimentosResponseDto> RecebimentosAsync(
            RecebimentosFiltroDto filtro,
            CancellationToken token = default)
        {
            var agoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var filtroBi = filtro.ParaFiltroBi();
            var periodo = RelatorioBiPeriodo.Resolver(filtroBi, agoraUtc);
            var agrupamento = RecebimentosCalculo.ResolverAgrupamento(filtro.Agrupamento, periodo.Dias);
            var criteria = RelatorioBiPeriodo.Criteria(filtroBi, periodo);
            var inicioPeriodoUtc = RelatorioBiPeriodo.InicioDoDiaUtc(periodo.Inicio);

            var recebimentos = await _repo.ListarRecebimentosAsync(criteria, token);
            var planejamento = await _repo.ListarPlanejamentoAsync(criteria, inicioPeriodoUtc, token);
            var orfaos = await _repo.ResumirOrfaosAsync(criteria, inicioPeriodoUtc, token);
            var meta = await MetaSemFiltroAsync(filtroBi, token);

            return RecebimentosCalculo.Montar(
                recebimentos,
                planejamento,
                orfaos,
                periodo,
                agrupamento,
                meta,
                RelatorioBiPeriodo.DiaLocal(agoraUtc));
        }

        public async Task<CapacidadeDocasResponseDto> CapacidadeDocasAsync(
            RelatorioBiFiltroDto filtro,
            CancellationToken token = default)
        {
            var periodo = RelatorioBiPeriodo.Resolver(filtro, _timeProvider.GetUtcNow().UtcDateTime);
            var vagas = await _repo.ListarVagasAsync(RelatorioBiPeriodo.Criteria(filtro, periodo), token);
            var docas = await _repo.ListarDocasAtivasAsync(filtro.UnidadeEntregaId, token);

            if (filtro.LocalDescargaId.HasValue)
                docas = docas.Where(d => d.Id == filtro.LocalDescargaId).ToList();

            if (filtro.ProdutoId.HasValue)
                docas = docas.Where(d => vagas.Any(v => v.LocalDescargaId == d.Id)).ToList();

            return CapacidadeDocasCalculo.Montar(vagas, docas, periodo);
        }

        private async Task<decimal?> MetaSemFiltroAsync(RelatorioBiFiltroDto filtro, CancellationToken token)
        {
            if (filtro.UnidadeEntregaId.HasValue || filtro.LocalDescargaId.HasValue
                || filtro.FornecedorId.HasValue || filtro.ProdutoId.HasValue)
            {
                return null;
            }

            var empresa = await _empresaRepo.GetById(_guard.GetEmpresaId(), token);
            return EmpresaMetaDiaria.ResolveKg(empresa?.Configuracoes);
        }

        public async Task<DashboardHojeResponseDto> DashboardHojeAsync(
            Guid? unidadeEntregaId,
            CancellationToken token = default)
        {
            var agoraUtc = _timeProvider.GetUtcNow().UtcDateTime;
            var hoje = RelatorioBiPeriodo.DiaLocal(agoraUtc);

            var criteria = new RelatorioBiCriteria(
                RelatorioBiPeriodo.InicioDoDiaUtc(hoje.AddDays(-7 * DashboardHojeCalculo.SemanasReferencia)),
                RelatorioBiPeriodo.InicioDoDiaUtc(hoje.AddDays(1)),
                unidadeEntregaId,
                null,
                null,
                null);

            var linhas = await _repo.ListarLinhasAsync(criteria, token);
            var docas = await _repo.ListarDocasAtivasAsync(unidadeEntregaId, token);
            var empresa = await _empresaRepo.GetById(_guard.GetEmpresaId(), token);
            var meta = unidadeEntregaId.HasValue ? null : EmpresaMetaDiaria.ResolveKg(empresa?.Configuracoes);

            return DashboardHojeCalculo.Montar(linhas, docas, agoraUtc, meta);
        }
    }
}
