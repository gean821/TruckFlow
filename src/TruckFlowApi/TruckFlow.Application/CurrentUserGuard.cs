using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Exceptions;
using TruckFlow.Application.Interfaces;

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
    }
}
