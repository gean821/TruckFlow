using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Dto.User
{
    public class UserCreateDto
    {
        public required Guid Id { get; set; } = Guid.NewGuid();
        public required string Email { get; set; }
        
        public required string Password { get; set; }
        public required string Username { get; set; }
        public required string Telefone { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
