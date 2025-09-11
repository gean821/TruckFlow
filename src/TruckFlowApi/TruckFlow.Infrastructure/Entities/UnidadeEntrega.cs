namespace TruckFlow.Infrastructure.Entities
{
    public class UnidadeEntrega
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
    }
}
