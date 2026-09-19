using Microsoft.EntityFrameworkCore;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlowApi.Infra.Repositories
{
    public class EntraGroupRoleMappingRepositorio : IEntraGroupRoleMappingRepositorio
    {
        private readonly AppDbContext _db;

        public EntraGroupRoleMappingRepositorio(AppDbContext db) => _db = db;

        public Task<List<EntraGroupRoleMapping>> GetAtivosByGroupIdsAsync(
            IReadOnlyCollection<string> entraGroupIds,
            CancellationToken token = default)
        {
            if (entraGroupIds.Count == 0)
                return Task.FromResult(new List<EntraGroupRoleMapping>());

            return _db.EntraGroupRoleMapping
                .Include(x => x.Empresa)
                .Where(x => x.Ativo && entraGroupIds.Contains(x.EntraGroupId))
                .ToListAsync(token);
        }
    }
}
