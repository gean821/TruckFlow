using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.User.Administrador;
using TruckFlow.Domain.Dto.User.Motorista;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IUsuarioService
    {
        public Task<UserAdminResponseDto> RegisterAdminAsync(UserAdminRegisterDto Usuario, CancellationToken token = default);
        public Task<LoginAdminResponseDto> LoginAdminAsync(UserAdminLoginDto dto, CancellationToken token = default);
        public Task<UserAdminResponseDto> GetAdminByIdAsync(Guid id, CancellationToken token);
        public Task<List<UserAdminResponseDto>> GetAllAsync(CancellationToken token = default);
        public Task<UserAdminResponseDto> UpdateAdminAsync(Guid id, UserAdminEditDto dto, CancellationToken token = default);
        public Task DeleteAdminAsync(Guid id, CancellationToken token = default);

        public Task<UserMotoristaResponseDto> RegisterMotoristaAsync(UserMotoristaRegisterDto Usuario, CancellationToken token = default);
        public Task<LoginMotoristaResponseDto> LoginMotoristaAsync(UserMotoristaLoginDto dto, CancellationToken token = default);
        public Task<UserMotoristaResponseDto> GetMotoristaByIdAsync(Guid id, CancellationToken token);
        public Task<UserMotoristaResponseDto> UpdateMotoristaAsync(Guid id, UserMotoristaUpdateDto dto, CancellationToken token = default);
        public Task DeleteMotoristaAsync(Guid id, CancellationToken token = default);

        public Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    }
}
