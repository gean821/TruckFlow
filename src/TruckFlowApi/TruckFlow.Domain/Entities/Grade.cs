using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public sealed class Grade : EntidadeBase
    {
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public required Produto Produto{ get; set; }

        public required Guid ProdutoId { get; set; }

        public DateOnly DataInicio { get; set; }
        public DateOnly DataFim { get; set; }
        public TimeOnly HoraInicial { get; set; }
        public TimeOnly HoraFinal { get; set; }
        public int IntervaloMinutos { get; set; }
    }
}
