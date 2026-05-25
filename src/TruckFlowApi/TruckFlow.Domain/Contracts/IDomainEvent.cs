namespace TruckFlow.Domain.Contracts
{
    public interface IDomainEvent
    {
        Guid EmpresaId { get; }
        DateTime OcorridoEm { get; }
        string IdempotencyKey { get; }
    }
}