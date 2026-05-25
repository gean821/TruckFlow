using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Application.Interfaces
{
    public interface INotificacaoStatsService
    {
        Task<NotificacaoStatsDto> GetAsync(CancellationToken token = default);
    }
}