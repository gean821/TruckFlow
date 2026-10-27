using TruckFlow.Domain.Dto.RelatorioBi;

namespace TruckFlow.Application.Interfaces
{
    public interface IRelatorioBiService
    {
        Task<VisaoGeralResponseDto> VisaoGeralAsync(RelatorioBiFiltroDto filtro, CancellationToken token = default);
    }
}
