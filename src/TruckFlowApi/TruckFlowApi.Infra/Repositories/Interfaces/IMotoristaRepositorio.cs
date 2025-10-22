using System;
using System.Collections.Generic;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IMotoristaRepositorio
    {
        public Task<Motorista> CreateMotorista(Motorista Motorista, CancellationToken token = default);
        public Task<Motorista> GetById(Guid id, CancellationToken token = default);
        public Task<List<Motorista>> GetAll(CancellationToken token = default);
        public Task<Motorista> UpdateMotorista(Guid id, Motorista motorista, CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
