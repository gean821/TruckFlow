using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Motorista
{
    public class UserMotoristaLoginDto
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
