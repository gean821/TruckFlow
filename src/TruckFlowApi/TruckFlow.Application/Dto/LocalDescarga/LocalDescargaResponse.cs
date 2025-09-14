using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Produto;

namespace TruckFlow.Application.Dto.LocalDescarga
{
    public class LocalDescargaResponse
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public ICollection<ProdutoResponse>? Produtos { get; set; }
    }
}
