using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface ILocalDescargaRepositorio
    {
        public Task<LocalDescarga> GetById(Guid id, CancellationToken token = default);
        public Task<List<LocalDescarga>> GetAll(CancellationToken token = default);
        public Task<LocalDescarga> CreateLocalDescarga(LocalDescarga local, CancellationToken token = default);
        public Task<LocalDescarga> Update(Guid id, LocalDescarga local,  CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
