using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Application
{
    public class CurrentUserGuard(ICurrentUserService currentUser)
    {
        private readonly ICurrentUserService _currentUser = currentUser;

        public Guid GetEmpresaId()
        {
            return _currentUser.EmpresaId
                ?? throw new BusinessException("Usuário não vinculado a empresa.");
        }

        public Guid GetUserId()
        {
            return _currentUser.UserIdOrNull
                ?? throw new UnauthorizedAccessException(
                    "Usuário não autenticado.");
        }

        public void EnsureMotorista()
        {
            if (!_currentUser.IsMotorista)
            {
                throw new UnauthorizedAccessException(
                    "Usuário não é motorista.");
            }
        }

        public Guid GetMotoristaId()
        {
            return _currentUser.MotoristaId
                ?? throw new BusinessException(
                    "Usuário não possui motorista vinculado.");
        }

        public void EnsureAdmin()
        {
            if (!_currentUser.IsAdmin)
            {
                throw new UnauthorizedAccessException(
                    "Usuário não é administrador.");
            }
        }
    }
}
