using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Dto.User.Motorista;

namespace TruckFlow.Application.Interfaces
{
    public interface IUsuarioService
    {
        Task<UserAdminResponseDto> RegisterAdminAsync(UserAdminRegisterDto Usuario, CancellationToken token = default);
        Task<LoginAdminResponseDto> LoginAdminAsync(UserAdminLoginDto dto, string? deviceInfo, string? ipAddress, CancellationToken token = default);
        Task<UserAdminResponseDto> GetAdminByIdAsync(Guid id, CancellationToken token);
        Task<PagedResponse<UserAdminResponseDto>> GetPagedUsers(UsuarioListQueryDto query, Guid empresaId, CancellationToken token = default);
        Task<List<string>> GetRolesAsync(CancellationToken token = default);
        Task<UserAdminResponseDto> UpdateAdminAsync(Guid id, UserAdminEditDto dto, CancellationToken token = default);
        Task<UserAdminResponseDto> SetAdminStatusAsync(Guid id, bool ativo, CancellationToken token = default);
        Task DeleteAdminAsync(Guid id, CancellationToken token = default);

        Task<UserMotoristaResponseDto> RegisterMotoristaAsync(UserMotoristaRegisterDto Usuario, CancellationToken token = default);
        Task<LoginMotoristaResponseDto> LoginMotoristaAsync(UserMotoristaLoginDto dto, string? deviceInfo, string? ipAddress, CancellationToken token = default);
        Task<UserMotoristaResponseDto> GetMotoristaByIdAsync(Guid id, CancellationToken token);
        Task<UserMotoristaResponseDto> UpdateMotoristaAsync(Guid id, UserMotoristaUpdateDto dto, CancellationToken token = default);
        Task DeleteMotoristaAsync(Guid id, CancellationToken token = default);

        Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

        Task AlterarSenhaComCodigoAsync(Guid usuarioId, string novaSenha, CancellationToken token = default);
        Task AlterarEmailComCodigoAsync(Guid usuarioId, string novoEmail, CancellationToken token = default);
        Task ConfirmarContaAsync(Guid usuarioId, CancellationToken token = default);
    }
}