using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class LoginAdminResponseDto
    {
        public required string Token { get; set; }
        public required UserAdminResponseDto Usuario { get; set; }
    }
}
