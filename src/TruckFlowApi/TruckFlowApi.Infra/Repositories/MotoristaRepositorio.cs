using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        public Task<Usuario> CreateUsuario(Usuario Usuario, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<Usuario> Delete(Guid id, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<List<Usuario>> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<Usuario> GetById(Guid id, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<Usuario> UpdateUsuario(Guid id, Usuario Usuario, CancellationToken token)
        {
            throw new NotImplementedException();
        }
    }
}
