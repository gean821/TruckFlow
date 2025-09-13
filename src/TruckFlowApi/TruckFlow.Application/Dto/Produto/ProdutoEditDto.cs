using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Dto.Produto
{
    public class ProdutoEditDto
    {
        public required string Nome { get; set; }
        public required Guid LocalDescargaId { get; set; }
    }
}
