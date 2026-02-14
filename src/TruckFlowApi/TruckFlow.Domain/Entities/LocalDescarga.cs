namespace TruckFlow.Domain.Entities
{
    public class LocalDescarga : EntidadeBase
    {
        public required string Nome { get; set; }
        public ICollection<Produto>? Produtos { get; set; } = [];
        public ICollection<Grade>? Grades { get; set; } = [];
    }
}
