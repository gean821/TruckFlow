using System;

namespace TruckFlow.Domain.Entities
{
    /// <summary>
    /// Mapeia um grupo do Entra ID (Aurora) para uma Empresa (fábrica/tenant) + Role (Admin/Gerente/Monitor/Portaria)
    /// do TruckFlow. Usado pelo provisionamento JIT no login federado
    /// </summary>
    public class EntraGroupRoleMapping
    {
        public Guid Id { get; set; }

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        /// <summary>Object ID do grupo no Entra ID (GUID gerado pela Aurora, não pelo TruckFlow).</summary>
        public required string EntraGroupId { get; set; }

        /// <summary>Nome legível do grupo, só para exibição na tela de mapeamento (ex.: "TruckFlow-Admin-Mandaguari").</summary>
        public required string EntraGroupNome { get; set; }

        /// <summary>Um dos valores de Roles.AdminLevel (Admin, Gerente, Monitor, Portaria).</summary>
        public required string RoleName { get; set; }

        public bool Ativo { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
