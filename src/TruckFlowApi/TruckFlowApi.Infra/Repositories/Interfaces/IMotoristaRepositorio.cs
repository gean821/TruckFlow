using System;
using System.Collections.Generic;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IMotoristaRepositorio
    {
        public Task<Motorista> CreateMotorista(Motorista Motorista, CancellationToken token);
        public Task<Motorista> GetById(Guid id, CancellationToken token);
        public Task<List<Motorista>> GetAll(CancellationToken token = default);
        public Task<Motorista> UpdateMotorista(Guid id, Motorista motorista, CancellationToken token);
        public Task Delete(Guid id, CancellationToken token);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
