using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Empresa;

namespace TruckFlow.Application.Interfaces
{
    public interface IEmpresaService
    {
        Task<EmpresaResponseDto> CreateEmpresa(
            EmpresaCreateDto dto,
            CancellationToken token = default);

        Task<EmpresaResponseDto> GetById(
            Guid id,
            CancellationToken token = default);

        Task<List<EmpresaResponseDto>> GetAll(
            CancellationToken token = default);

        Task<EmpresaResponseDto> Update(
            Guid id,
            EmpresaUpdateDto dto,
            CancellationToken token = default);
        Task Desativar(
            Guid id,
            CancellationToken token = default);
    }
}
