namespace TruckFlowApi.Infra.Outbox
{
    public interface IOutboxEventTypeResolver
    {
        Type Resolve(string eventTypeName);
    }
}
