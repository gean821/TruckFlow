using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class Produto : EntidadeBase, IEmpresaScoped
    {
        public required string Nome { get; set; }
        public required LocalDescarga LocalDescarga{ get; set; }
        public required Guid LocalDescargaId { get; set; }

        public ICollection<ItemPlanejamento>? ItemPlanejamentos { get; set; } = [];

        public string? CodigoEan { get; set; }

        public ICollection<ProdutoFornecedor> ProdutoFornecedores { get; set; } = [];

        public ICollection<Grade>? Grades { get; set; } = [];

        public required Guid EmpresaId { get; set; }
        public required Empresa Empresa { get; set; }
    }
}
