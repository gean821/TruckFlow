using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRelatorioBiRepositorio
    {
        Task<List<RelatorioBiLinha>> ListarLinhasAsync(RelatorioBiCriteria criteria, CancellationToken token = default);

        Task<int> ContarDocasAtivasAsync(Guid? unidadeEntregaId, Guid? localDescargaId, CancellationToken token = default);

        Task<List<RelatorioBiDoca>> ListarDocasAtivasAsync(Guid? unidadeEntregaId, CancellationToken token = default);

        Task<List<RecebimentoLinha>> ListarRecebimentosAsync(RelatorioBiCriteria criteria, CancellationToken token = default);

        Task<List<PlanejamentoItemLinha>> ListarPlanejamentoAsync(RelatorioBiCriteria criteria, DateTime periodoInicioUtc, CancellationToken token = default);

        Task<List<CapacidadeVagaLinha>> ListarVagasAsync(RelatorioBiCriteria criteria, CancellationToken token = default);

        Task<(int Quantidade, decimal Kg)> ResumirOrfaosAsync(RelatorioBiCriteria criteria, DateTime periodoInicioUtc, CancellationToken token = default);
    }
}
