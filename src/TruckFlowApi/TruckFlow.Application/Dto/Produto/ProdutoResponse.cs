using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Dto.Produto
{
    public class ProdutoResponse
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public required string LocalDescarga { get; set; }

        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
