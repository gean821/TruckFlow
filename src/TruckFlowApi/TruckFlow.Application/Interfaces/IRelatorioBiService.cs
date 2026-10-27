using TruckFlow.Domain.Dto.Dashboard;
using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlow.Application.Interfaces
{
    public interface IRelatorioBiService
    {
        Task<VisaoGeralResponseDto> VisaoGeralAsync(RelatorioBiFiltroDto filtro, CancellationToken token = default);

        Task<DashboardHojeResponseDto> DashboardHojeAsync(Guid? unidadeEntregaId, CancellationToken token = default);
    }
}
