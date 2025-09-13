using TruckFlow.Domain.Entities;

namespace TruckFlow.Infrastructure.Entities
{
    public class UnidadeEntrega : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
    }
}
