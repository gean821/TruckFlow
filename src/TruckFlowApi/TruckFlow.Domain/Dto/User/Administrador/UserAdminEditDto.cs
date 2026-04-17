using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class UserAdminEditDto 
    {
        public string? Email { get; set; }
        
        public string? Password { get; set; }
        public string? Username { get; set; }
        public string? Telefone { get; set; }

    }
}
