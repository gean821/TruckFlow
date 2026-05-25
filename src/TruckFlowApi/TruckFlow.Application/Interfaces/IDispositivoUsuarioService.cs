using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Application.Interfaces
{
    public interface IDispositivoUsuarioService
    {
        Task RegistrarAsync(RegistrarDispositivoDto dto, CancellationToken ct);
    }
}