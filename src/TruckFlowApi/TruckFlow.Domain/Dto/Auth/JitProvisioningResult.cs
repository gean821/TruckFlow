using TruckFlow.Domain.Entities;

namespace TruckFlow.Domain.Dto.Auth
{
    public enum JitProvisioningFailure
    {
        /// <summary>Usuário autenticado no Entra ID, mas nenhum grupo bate com EntraGroupRoleMapping.</summary>
        SemGrupoMapeado,

        /// <summary>Grupos mapeados resolvem para mais de uma Empresa — ambíguo, tratado como erro de configuração.</summary>
        GruposAmbiguos,

        /// <summary>A Empresa resolvida não tem "EntraId" em Configuracoes.authMethods (ENTRA-06).</summary>
        EmpresaNaoPermiteEntraId,
    }

    public sealed record JitProvisioningResult
    {
        public bool Success { get; private init; }
        public Usuario? Usuario { get; private init; }
        public JitProvisioningFailure? FailureReason { get; private init; }

        public static JitProvisioningResult Ok(Usuario usuario) =>
            new() { Success = true, Usuario = usuario };

        public static JitProvisioningResult Fail(JitProvisioningFailure reason) =>
            new() { Success = false, FailureReason = reason };
    }
}
