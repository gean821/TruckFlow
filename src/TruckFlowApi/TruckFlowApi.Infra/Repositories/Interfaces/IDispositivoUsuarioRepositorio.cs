using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IDispositivoUsuarioRepositorio
    {
        Task<DispositivoUsuario?> GetByTokenAsync(string expoPushToken, CancellationToken token = default);

        Task<List<DispositivoUsuario>> GetAtivosByUsuarioAsync(Guid usuarioId, CancellationToken token = default);

        Task<DispositivoUsuario> AddAsync(DispositivoUsuario dispositivo, CancellationToken token = default);

        Task<DispositivoUsuario> UpdateAsync(DispositivoUsuario dispositivo, CancellationToken token = default);
    }
}