using System;
using System.Collections.Generic;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IUsuarioRepositorio
    {
        public Task<Usuario> CreateUsuario(Usuario Usuario, CancellationToken token);
        public Task<Usuario> GetById(Guid id, CancellationToken token);
        public Task<List<Usuario>> GetAll();
        public Task<Usuario> UpdateUsuario(Guid id, Usuario Usuario, CancellationToken token);
        public Task<Usuario> Delete(Guid id, CancellationToken token);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}
