namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoExpirationService
    {
        Task<int> ExpirarVencidosAsync(CancellationToken cancellationToken = default);
    }
}
