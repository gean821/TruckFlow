using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface INotificacaoEntregaRepositorio
    {
        Task<NotificacaoEntrega?> ClaimNextPushPendenteAsync(CancellationToken token = default);

        Task<List<NotificacaoEntrega>> ClaimLotePushPendenteReceiptAsync(
            int limit,
            TimeSpan idadeMinima,
            CancellationToken token = default);

        Task<Notificacao?> GetNotificacaoByEntregaAsync(Guid entregaId, CancellationToken token = default);

        Task MarcarReceiptVerificadoEmLoteAsync(
            IReadOnlyCollection<Guid> entregaIds,
            CancellationToken token = default);

        Task SaveChangesAsync(CancellationToken token = default);
    }
}