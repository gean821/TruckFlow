using System.Threading.Channels;
using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Application.Notificacoes
{
    public interface INotificationConnectionManager
    {
        Channel<NotificacaoEventDto> Register(Guid usuarioId);
        void Unregister(Guid usuarioId, Channel<NotificacaoEventDto> channel);
        void PublishToUser(Guid usuarioId, NotificacaoEventDto evt);
    }
}