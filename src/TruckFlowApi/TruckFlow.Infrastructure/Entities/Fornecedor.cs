using TruckFlow.Domain.Entities;

namespace TruckFlow.Infrastructure.Entities
{
    public class Fornecedor : EntidadeBase
    {
        public required string Nome { get; set; }
    }
}