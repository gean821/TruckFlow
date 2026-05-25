using System.Collections.Concurrent;
using System.Reflection;
using TruckFlow.Domain.Contracts;

namespace TruckFlowApi.Infra.Outbox
{
    public sealed class OutboxEventTypeResolver : IOutboxEventTypeResolver
    {
        private static readonly ConcurrentDictionary<string, Type> _cache = new();

        private static readonly Lazy<IReadOnlyDictionary<string, Type>> _knownEvents = new(BuildKnownEventsIndex);

        public Type Resolve(string eventTypeName)
        {
            if (string.IsNullOrWhiteSpace(eventTypeName))
            {
                throw new ArgumentException("EventTypeName não pode ser vazio.", nameof(eventTypeName));
            }

            return _cache.GetOrAdd(eventTypeName, name =>
            {
                if (_knownEvents.Value.TryGetValue(name, out var type))
                {
                    return type;
                }

                throw new InvalidOperationException(
                    $"Nenhum tipo IDomainEvent registrado para '{name}'. " +
                    "Verifique se o evento foi criado em TruckFlow.Domain.Events ou se o assembly foi carregado.");
            });
        }

        private static IReadOnlyDictionary<string, Type> BuildKnownEventsIndex()
        {
            var domainEventInterface = typeof(IDomainEvent);
            var assembly = domainEventInterface.Assembly;

            return assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && domainEventInterface.IsAssignableFrom(t))
                .ToDictionary(t => t.FullName!, t => t);
        }
    }
}