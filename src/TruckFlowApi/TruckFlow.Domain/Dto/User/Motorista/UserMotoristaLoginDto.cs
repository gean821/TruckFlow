using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Motorista
{
    public class UserMotoristaLoginDto
    {
        public required string Login { get; set; } = null!;
        public required string Password { get; set; }
    }
}
