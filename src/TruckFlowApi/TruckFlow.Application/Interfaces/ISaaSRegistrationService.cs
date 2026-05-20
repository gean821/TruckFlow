using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Domain.Dto.User.Administrador;

namespace TruckFlow.Application.Interfaces
{
    public interface ISaaSRegistrationService
    {
        Task<LoginAdminResponseDto> RegisterAsync(
            RegisterEmpresaAdminDto dto,
            string? deviceInfo,
            string? ipAddress,
            CancellationToken token = default);
    }
}