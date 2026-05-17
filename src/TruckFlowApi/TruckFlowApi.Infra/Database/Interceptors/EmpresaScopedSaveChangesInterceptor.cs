using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TruckFlow.Domain.Contracts;

namespace TruckFlowApi.Infra.Database.Interceptors
{
    /// <summary>
    /// Defesa em profundidade contra vazamento cross-tenant em operações de escrita.
    /// O HasQueryFilter (AppDbContext) protege LEITURA; este interceptor protege INSERT/UPDATE/DELETE
    /// em entidades <see cref="IEmpresaScoped"/>.
    ///
    /// Comportamento:
    ///   - Added:    se EmpresaId vazio, popula com o tenant atual; se preenchido com outro tenant, throw.
    ///   - Modified: rejeita modificações em entidade de outro tenant; rejeita troca de EmpresaId.
    ///   - Deleted:  rejeita delete de entidade de outro tenant.
    ///
    /// Se não houver tenant no contexto (ex.: worker sem HttpContext) e a operação envolver entidade
    /// IEmpresaScoped, a leitura de <see cref="IEmpresaContext.EmpresaId"/> lançará — comportamento desejado.
    /// </summary>
    public sealed class EmpresaScopedSaveChangesInterceptor : SaveChangesInterceptor
    {
        private readonly IEmpresaContext _empresaContext;

        public EmpresaScopedSaveChangesInterceptor(IEmpresaContext empresaContext)
        {
            _empresaContext = empresaContext;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            EnforceTenantScope(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            EnforceTenantScope(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private void EnforceTenantScope(DbContext? ctx)
        {
            if (ctx is null)
            {
                return;
            }

            var entries = ctx.ChangeTracker
                .Entries<IEmpresaScoped>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            var currentTenant = _empresaContext.EmpresaId;
            const string empresaIdName = nameof(IEmpresaScoped.EmpresaId);

            foreach (var entry in entries)
            {
                var entityName = entry.Entity.GetType().Name;

                switch (entry.State)
                {
                    case EntityState.Added:
                        if (entry.Entity.EmpresaId == Guid.Empty)
                        {
                            entry.Entity.EmpresaId = currentTenant;
                        }
                        else if (entry.Entity.EmpresaId != currentTenant)
                        {
                            throw new UnauthorizedAccessException(
                                $"Tentativa de criar {entityName} para tenant {entry.Entity.EmpresaId}; tenant atual é {currentTenant}.");
                        }
                        break;

                    case EntityState.Modified:
                    {
                        var originalEmpresaId = (Guid)entry.OriginalValues[empresaIdName]!;

                        if (originalEmpresaId != currentTenant)
                        {
                            throw new UnauthorizedAccessException(
                                $"Tentativa de modificar {entityName} do tenant {originalEmpresaId}; tenant atual é {currentTenant}.");
                        }

                        if (entry.Entity.EmpresaId != originalEmpresaId)
                        {
                            throw new UnauthorizedAccessException(
                                $"Tentativa de transferir {entityName} de {originalEmpresaId} para {entry.Entity.EmpresaId}.");
                        }
                        break;
                    }

                    case EntityState.Deleted:
                    {
                        var deletingEmpresaId = (Guid)entry.OriginalValues[empresaIdName]!;
                        if (deletingEmpresaId != currentTenant)
                        {
                            throw new UnauthorizedAccessException(
                                $"Tentativa de deletar {entityName} do tenant {deletingEmpresaId}; tenant atual é {currentTenant}.");
                        }
                        break;
                    }
                }
            }
        }
    }
}