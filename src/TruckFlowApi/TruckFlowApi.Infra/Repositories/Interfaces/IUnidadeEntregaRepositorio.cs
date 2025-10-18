using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IUnidadeEntregaRepositorio
    {
        public Task<UnidadeEntrega> GetById(Guid id, CancellationToken token = default);
        public Task<List<UnidadeEntrega>> GetAll(CancellationToken token = default);
        public Task<UnidadeEntrega> CreateUnidadeEntrega(UnidadeEntrega unidade, CancellationToken token = default);
        public Task<UnidadeEntrega> Update(Guid id, UnidadeEntrega unidade,CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    } 
}
