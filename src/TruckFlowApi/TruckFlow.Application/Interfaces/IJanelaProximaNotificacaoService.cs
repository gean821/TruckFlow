namespace TruckFlow.Application.Interfaces
{
    public interface IJanelaProximaNotificacaoService
    {
        Task<int> EnviarLembretesAsync(
            Guid empresaId,
            CancellationToken cancellationToken = default
        );
    }
}
