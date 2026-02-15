using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class EmpresaRepositorio : IEmpresaRepositorio
    {
        public Task<Empresa> CreateEmpresa(
            Empresa local,
            CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task Delete(
            Guid id,
            CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<List<Empresa>> GetAll(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Empresa> GetByCnpj(string cnpj, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Empresa> GetById(Guid id, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task SaveChangesAsync(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Empresa> Update(Guid id, Empresa local, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
