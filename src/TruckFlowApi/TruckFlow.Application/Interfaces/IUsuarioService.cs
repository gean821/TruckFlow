using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.User;

namespace TruckFlow.Application.Interfaces
{
    public interface IUsuarioService
    {
        public Task<UserResponseDto> CreateUsuario(UserCreateDto Usuario);
        public Task<UserResponseDto> GetById(Guid id, CancellationToken token);
        public Task<List<Usuario>> GetAll();
        public Task<Usuario> UpdateUsuario(Guid id, Usuario Usuario, CancellationToken token);
        public Task<Usuario> Delete(Guid id, CancellationToken token);
    }
}
