using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotaFiscalItemRepositorio
    {
        Task<NotaFiscalItem?> GetByIdAsync(Guid id, CancellationToken token = default);
        Task SaveChangesAsync(CancellationToken token = default);
    }
}
