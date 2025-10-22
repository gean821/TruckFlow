using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Produto
{
    public class ProdutoCreateDto
    {
        public required string Nome { get; set; }
        public required Guid LocalDescargaId { get; set; }
    }
}
