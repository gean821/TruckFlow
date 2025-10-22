using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlow.Interfaces
{
    public interface IAuthService
    {
        public string GenerateToken(Usuario usuario);
    }
}
