using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Produto
{
    public class ProdutoEditDto
    {
        public string? Nome { get; set; }
        public Guid? LocalDescargaId { get; set; }
    }
}
