namespace TruckFlow.Application.Entities
{
    public class LocalDescarga : EntidadeBase
    {
        public required string Nome { get; set; }
        public ICollection<Produto>? Produtos { get; set; } = new List<Produto>();
    }
}
