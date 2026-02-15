using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Contracts;

namespace TruckFlow.Domain.Entities
{
    public class ProdutoFornecedor: IEmpresaScoped
    {

        public required Guid FornecedorId { get; set; }
        public required Guid ProdutoId { get; set; }

        public required Produto Produto { get; set; }
        public required Fornecedor Fornecedor { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt{ get; set; }
        public DateTime? DeletedAt { get; set; }
        public Guid EmpresaId { get; set; }
        public required Empresa Empresa { get; set; }

    }
}
