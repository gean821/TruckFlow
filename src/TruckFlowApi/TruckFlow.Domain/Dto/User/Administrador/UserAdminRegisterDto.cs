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
        public string? PhotoUrl { get; set; }
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Role do usuário a ser criado (ex.: Admin, Operador). Default Admin para
        /// preservar callers existentes (SaaS registration, etc).
        /// </summary>
        public string Role { get; set; } = "Admin";

        public Guid EmpresaId { get; set; }
    }
}
