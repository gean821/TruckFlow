using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Domain.Dto.User
{
    public class LoginResponseDto
    {
        public required string Token { get; set; }
        public required Usuario Usuario{ get; set; }
    }
}
