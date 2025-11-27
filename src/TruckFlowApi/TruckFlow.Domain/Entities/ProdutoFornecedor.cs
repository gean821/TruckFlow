using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public class ProdutoFornecedor 
    {

        public required Guid FornecedorId { get; set; }
        public required Guid ProdutoId { get; set; }

        public required Produto Produto { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt{ get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
