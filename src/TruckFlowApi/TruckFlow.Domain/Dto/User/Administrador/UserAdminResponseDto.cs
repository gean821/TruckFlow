using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class UserAdminResponseDto 
    {
        public required Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public string? NomeReal { get; set; }
        public required string Role { get; set; }

        public string? Empresa { get; set; }
        public Guid? EmpresaId { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
