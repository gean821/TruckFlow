using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IEmpresaRepositorio
    {
        public Task<Empresa?> GetById(Guid id, CancellationToken token = default);
        public Task<Empresa?> GetByCnpj(string cnpj, CancellationToken token = default);
        public Task<List<Empresa>> GetAll(CancellationToken token = default);
        public Task<Empresa> CreateEmpresa(Empresa local, CancellationToken token = default);
        public Task<Empresa> Update(Empresa local, CancellationToken token = default);
        public Task Delete(Empresa empresa, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
