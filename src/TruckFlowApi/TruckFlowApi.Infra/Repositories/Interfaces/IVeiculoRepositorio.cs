using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    internal interface IVeiculoRepositorio
    {
        public Task<Veiculo> GetById(Guid id, CancellationToken token = default);
        public Task<List<Veiculo>> GetAll(CancellationToken token = default);
        public Task<Veiculo> CreateVeiculo(Veiculo veiculo, CancellationToken token = default);
        public Task<Veiculo> Update(Guid id, Veiculo veiculo, CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
