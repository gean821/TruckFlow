using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.User.Administrador
{
    public class UserAdminRegisterDto
    {
        public Guid? Id { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string Username { get; set; }
        public required string NomeReal { get; set; }
        public required string Telefone { get; set; }
        public DateTime? CreatedAt { get; set; }

        public Guid EmpresaId { get; set; }
    }
}
