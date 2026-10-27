using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlow.Application.Interfaces
{
    public interface IRelatorioBiService
    {
        Task<VisaoGeralResponseDto> VisaoGeralAsync(RelatorioBiFiltroDto filtro, CancellationToken token = default);

        Task<DashboardHojeResponseDto> DashboardHojeAsync(Guid? unidadeEntregaId, CancellationToken token = default);

        Task<RecebimentosResponseDto> RecebimentosAsync(RecebimentosFiltroDto filtro, CancellationToken token = default);

        Task<CapacidadeDocasResponseDto> CapacidadeDocasAsync(RelatorioBiFiltroDto filtro, CancellationToken token = default);

        Task<ProdutividadeResponseDto> ProdutividadeAsync(ProdutividadeFiltroDto filtro, CancellationToken token = default);

        Task<ProdutividadePatioDto> PatioAsync(ProdutividadePatioFiltroDto filtro, CancellationToken token = default);
    }
}
