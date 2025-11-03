using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Domain.Dto.Fornecedor
{
    public class FornecedorResponse
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public ICollection<ProdutoResponse>? Produtos { get; set; } = [];
        public required DateTime CreatedAt { get; set; }
    }
}
