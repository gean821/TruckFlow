namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoExpirationService
    {
        Task<int> ExpirarVencidosAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default
        );
    }
}
