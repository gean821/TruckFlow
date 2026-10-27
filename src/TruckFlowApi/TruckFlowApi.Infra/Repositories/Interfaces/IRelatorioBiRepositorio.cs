using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRelatorioBiRepositorio
    {
        Task<List<RelatorioBiLinha>> ListarLinhasAsync(RelatorioBiCriteria criteria, CancellationToken token = default);

        Task<int> ContarDocasAtivasAsync(Guid? unidadeEntregaId, Guid? localDescargaId, CancellationToken token = default);
    }
}
