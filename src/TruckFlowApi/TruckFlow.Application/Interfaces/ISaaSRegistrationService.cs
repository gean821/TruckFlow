using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Empresa;
using TruckFlow.Domain.Dto.User.Administrador;

namespace TruckFlow.Application.Interfaces
{
    public interface ISaaSRegistrationService
    {
        Task<LoginAdminResponseDto> RegisterAsync(
            RegisterEmpresaAdminDto dto,
            CancellationToken token = default);
    }
}
