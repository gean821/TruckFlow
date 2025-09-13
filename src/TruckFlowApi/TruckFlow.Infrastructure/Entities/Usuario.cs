using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Infrastructure.Entities;

namespace TruckFlow.Domain.Entities
{
    public class Usuario : IdentityUser   //aqui fica o usuario base do identity, autenticação, roles, está tudo aqui.
    { 
        public Administrador? Administrador { get; set; }
        public Motorista? Motorista{ get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
