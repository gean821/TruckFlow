using TruckFlow.Application.Interfaces;
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

        public RelatorioBiService(
            IRelatorioBiRepositorio repo,
            IEmpresaRepositorio empresaRepo,
            CurrentUserGuard guard,
            TimeProvider timeProvider)
        {
            _repo = repo;
            _empresaRepo = empresaRepo;
            _guard = guard;
            _timeProvider = timeProvider;
        }

        public async Task<VisaoGeralResponseDto> VisaoGeralAsync(
            RelatorioBiFiltroDto filtro,
            CancellationToken token = default)
        {
            var periodo = RelatorioBiPeriodo.Resolver(filtro, _timeProvider.GetUtcNow().UtcDateTime);
            var linhas = await _repo.ListarLinhasAsync(RelatorioBiPeriodo.Criteria(filtro, periodo), token);
            var docas = await _repo.ContarDocasAtivasAsync(filtro.UnidadeEntregaId, filtro.LocalDescargaId, token);
            var empresa = await _empresaRepo.GetById(_guard.GetEmpresaId(), token);

            var filtrado = filtro.UnidadeEntregaId.HasValue
                || filtro.LocalDescargaId.HasValue
                || filtro.FornecedorId.HasValue
                || filtro.ProdutoId.HasValue;

            var meta = filtrado ? null : EmpresaMetaDiaria.ResolveKg(empresa?.Configuracoes);

            return VisaoGeralCalculo.Montar(linhas, periodo, meta, docas);
        }
    }
}
