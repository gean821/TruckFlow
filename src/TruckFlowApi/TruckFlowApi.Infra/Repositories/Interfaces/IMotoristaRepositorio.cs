using System;
using System.Collections.Generic;
using TruckFlow.Domain.Dto.Veiculo;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IMotoristaRepositorio
    {
        public Task<List<Veiculo>> GetMeusVeiculos(Guid motoristaId, CancellationToken token = default);

        public Task<Motorista?> GetByUsuarioId(Guid usuarioId, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
