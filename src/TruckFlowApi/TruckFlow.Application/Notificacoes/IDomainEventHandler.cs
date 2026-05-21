using TruckFlow.Domain.Contracts;

namespace TruckFlow.Application.Notificacoes
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
