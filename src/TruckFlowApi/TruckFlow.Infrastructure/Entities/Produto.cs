namespace TruckFlow.Infrastructure.Entities
{
    public class Produto
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public required LocalDescarga LocalDescarga{ get; set; }
    }
}
