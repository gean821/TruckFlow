using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using TruckFlow.Domain.Contracts;

namespace TruckFlowApi.Infra.Outbox
{
    public static class OutboxEventSerializer
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string Serialize(IDomainEvent domainEvent)
            => JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options);

        public static IDomainEvent Deserialize(string payload, Type eventType)
            => (IDomainEvent)JsonSerializer.Deserialize(payload, eventType, Options)!;
    }
}