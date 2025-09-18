using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Veiculo;

namespace TruckFlow.Application.Interfaces
{
    public interface IVeiculoService
    {
        public Task<VeiculoResponse> GetById(Guid id, CancellationToken token = default);
        public Task<List<VeiculoResponse>> GetAll(CancellationToken token = default);
        public Task<VeiculoResponse> CreateVeiculo(VeiculoCreateDto veiculo, CancellationToken token = default);
        public Task<VeiculoResponse> UpdateVeiculo(Guid id, VeiculoUpdateDto veiculo, CancellationToken token = default);
        public Task DeleteVeiculo(Guid id, CancellationToken token = default);
    }
}
