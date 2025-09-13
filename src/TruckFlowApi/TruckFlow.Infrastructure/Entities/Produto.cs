using TruckFlow.Domain.Entities;

namespace TruckFlow.Infrastructure.Entities
{
    public class Produto : EntidadeBase
    {
        public required string Nome { get; set; }
        public required LocalDescarga LocalDescarga{ get; set; }
    }
}
