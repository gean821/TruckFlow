using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IOutboxEventRepositorio
    {
        Task<OutboxStatsDto> GetStatsAsync(CancellationToken token = default);
    }
}