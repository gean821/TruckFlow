using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User
{
    public class UserResponseDto 
    {
        public required Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
