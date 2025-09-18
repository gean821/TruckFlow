using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Dto.Fornecedor
{
    public class FornecedorCreateDto
    {
        public required string Nome { get; set; }
        public required Guid ProdutoId  { get; set; }
    }
}
