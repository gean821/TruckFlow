namespace TruckFlow.Domain.Entities
{
    public class LocalDescarga : EntidadeBase
    {
        public required string Nome { get; set; }
        public required Produto Produto { get; set; }
        public required Guid ProdutoId { get; set; }
    }
}
