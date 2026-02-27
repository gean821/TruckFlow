using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Domain.Entities
{
    public class Usuario : IdentityUser<Guid>, IEmpresaScoped   //aqui fica o usuario base do identity, autenticação, roles, está tudo aqui.
    { 
        public Administrador? Administrador { get; set; }
        public Motorista? Motorista{ get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public ICollection<Agendamento>? Agendamentos { get; set; }
    }
}
