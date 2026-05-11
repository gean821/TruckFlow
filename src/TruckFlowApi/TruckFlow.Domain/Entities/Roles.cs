using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public static class Roles
    {
        public const string Admin = nameof(Admin);
        public const string Motorista = nameof(Motorista);
        public const string Gerente = nameof(Gerente);
        public const string Monitor = nameof(Monitor);
        public const string Portaria = nameof(Portaria);

        public static readonly IReadOnlyList<string> All =
        [
            Admin,
            Motorista,
            Gerente,
            Monitor,
            Portaria,
        ];

        public static readonly IReadOnlyList<string> AdminLevel =
        [
            Admin,
            Gerente,
            Monitor,
            Portaria,
        ];
    }

    /// <summary>
    /// Combinações pré-definidas para uso em <c>[Authorize(Roles = ...)]</c>.
    /// Centraliza "quem pode o quê" pra facilitar revisão e mudanças.
    /// </summary>
    public static class RoleGroups
    {
        // Programação operacional: criar/editar grades, agendamentos avulsos, gerenciar fluxo
        public const string CanManageGrade =
            $"{Roles.Admin},{Roles.Gerente},{Roles.Monitor}";

        // Operação de portaria: check-in/check-out, mudança de status do agendamento
        public const string CanCheckIn =
            $"{Roles.Admin},{Roles.Gerente},{Roles.Monitor},{Roles.Portaria}";

        // Visualização de grades e agendamentos (read-only inclusive)
        public const string CanViewSchedule =
            $"{Roles.Admin},{Roles.Gerente},{Roles.Monitor},{Roles.Portaria}";

        // Gerenciamento de usuários e roles (perfil sênior)
        public const string CanManageUsers =
            $"{Roles.Admin},{Roles.Gerente}";

        // Cadastros de domínio (fornecedor, produto, doca, unidade)
        public const string CanManageMasterData =
            $"{Roles.Admin},{Roles.Gerente}";

        // Auditoria (ler logs)
        public const string CanViewAuditLogs =
            $"{Roles.Admin},{Roles.Gerente}";
    }
}
