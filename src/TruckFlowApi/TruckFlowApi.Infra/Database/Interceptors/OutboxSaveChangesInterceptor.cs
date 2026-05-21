using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Outbox;

namespace TruckFlowApi.Infra.Database.Interceptors
{
    public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            CollectAndEnqueueEvents(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            CollectAndEnqueueEvents(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        private static void CollectAndEnqueueEvents(DbContext? ctx)
        {
            if (ctx is null)
            {
                return;
            }

            var entries = ctx.ChangeTracker
                .Entries<EntidadeBase>()
                .Where(e => e.Entity.DomainEvents.Count > 0)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var outboxEvents = new List<OutboxEvent>();

            foreach (var entry in entries)
            {
                foreach (var domainEvent in entry.Entity.DomainEvents)
                {
                    outboxEvents.Add(new OutboxEvent
                    {
                        EventType = domainEvent.GetType().FullName!,
                        Payload = OutboxEventSerializer.Serialize(domainEvent),
                        IdempotencyKey = domainEvent.IdempotencyKey,
                        EmpresaId = domainEvent.EmpresaId,
                        OcorridoEm = domainEvent.OcorridoEm,
                        CreatedAt = now,
                    });
                }

                entry.Entity.ClearDomainEvents();
            }

            ctx.AddRange(outboxEvents);
        }
    }
}