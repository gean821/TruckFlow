using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class ProdutoFornecedor : IEmpresaScoped
    {
        public required Guid FornecedorId { get; set; }
        public required Guid ProdutoId { get; set; }

        public required Produto Produto { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        /// <summary>
        /// Código que o fornecedor usa pra esse produto nas notas fiscais dele (cProd).
        /// Chave da amarração XML→catálogo: quando NF do fornecedor X chega com cProd Y,
        /// o sistema acha o produto cadastrado via esse mapping. Auto-learning preenche
        /// quando admin confirma manualmente.
        /// </summary>
        public string? CodigoFornecedor { get; set; }

        public string? EanFornecedor { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid EmpresaId { get; set; }
        public required Empresa Empresa { get; set; }
    }
}
