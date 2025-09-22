using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Dto.Fornecedor
{
    public class FornecedorResponse
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public string? Produto { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
