using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IEntraGroupRoleMappingRepositorio
    {
        /// <summary>
        /// Retorna os mapeamentos ativos cujo EntraGroupId está entre os grupos informados
        /// (tipicamente os grupos presentes na claim "groups" do token do Entra ID).
        /// </summary>
        Task<List<EntraGroupRoleMapping>> GetAtivosByGroupIdsAsync(
            IReadOnlyCollection<string> entraGroupIds,
            CancellationToken token = default);
    }
}
