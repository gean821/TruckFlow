using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Channels;
using TruckFlow.Application.Notificacoes;
using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Extensions.Notificacao
{
    public sealed class NotificationConnectionManager : INotificationConnectionManager
    {
        private const int ChannelCapacity = 100;

        private readonly ConcurrentDictionary<Guid, ImmutableList<Channel<NotificacaoEventDto>>> _connections = new();

        public Channel<NotificacaoEventDto> Register(Guid usuarioId)
        {
            var channel = Channel.CreateBounded<NotificacaoEventDto>(new BoundedChannelOptions(ChannelCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false,
            });

            _connections.AddOrUpdate(
                usuarioId,
                _ => ImmutableList.Create(channel),
                (_, list) => list.Add(channel));

            return channel;
        }

        public void Unregister
            (
                Guid usuarioId,
                Channel<NotificacaoEventDto> channel
            )
        {
            _connections.AddOrUpdate(
                usuarioId,
                _ => ImmutableList<Channel<NotificacaoEventDto>>.Empty,
                (_, list) => list.Remove(channel));

            if (_connections.TryGetValue(usuarioId, out var current) && current.IsEmpty)
            {
                _connections.TryRemove(KeyValuePair.Create(usuarioId, current));
            }

            channel.Writer.TryComplete();
        }

        public void PublishToUser
            (
                Guid usuarioId,
                NotificacaoEventDto evt
            )
        {
            if (!_connections.TryGetValue(usuarioId, out var list))
            {
                return;
            }

            foreach (var channel in list)
            {
                channel.Writer.TryWrite(evt);
            }
        }

        public int ActiveConnectionCount()
        {
            var total = 0;
            foreach (var pair in _connections)
            {
                total += pair.Value.Count;
            }
            return total;
        }

        public int ActiveUserCount() => _connections.Count;
    }
}