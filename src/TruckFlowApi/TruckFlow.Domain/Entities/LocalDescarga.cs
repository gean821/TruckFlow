using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class LocalDescarga : EntidadeBase, IEmpresaScoped
    {
        public required string Nome { get; set; }
        public ICollection<Produto>? Produtos { get; set; } = [];
        public ICollection<Grade>? Grades { get; set; } = [];
        public Guid UnidadeEntregaId { get; set; }
        public required UnidadeEntrega UnidadeEntrega { get; set; }
        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public bool? Ativa { get; set; } = true;

    }
}
