namespace TruckFlow.Domain.Entities
{
    public class Produto : EntidadeBase
    {
        public required string Nome { get; set; }
        public required LocalDescarga LocalDescarga{ get; set; }
        public required Guid LocalDescargaId { get; set; }

        public ICollection<Fornecedor>? Fornecedores { get; set; } = [];

        public ICollection<Grade>? Grades { get; set; } = [];
    }
}
