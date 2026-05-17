using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IUsuarioRepositorio
    {
        Task<Usuario?> GetForTokenRefreshAsync(Guid userId, CancellationToken token = default);
    }
}
