using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.User;
using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IUsuarioService
    {
        public Task<UsuarioDto> CreateUsuario(UserCreateDto Usuario);
        public Task<UsuarioDto> GetById(Guid id, CancellationToken token);
        public Task<List<Usuario>> GetAll();
        public Task<Usuario> UpdateUsuario(Guid id, Usuario Usuario, CancellationToken token);
        public Task<Usuario> Delete(Guid id, CancellationToken token);
    }
}
