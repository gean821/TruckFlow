using TruckFlow.Domain.Entities;

namespace TruckFlow.Infrastructure.Entities
{
    public class Notificacao : EntidadeBase
    {
        public required string Descricao { get; set; }
    }
}