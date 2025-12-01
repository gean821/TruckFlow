using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public sealed class Grade : EntidadeBase
    {
        public ICollection<Fornecedor> Fornecedores { get; set; } = [];
        public ICollection<Produto> Produtos { get; set; } = [];
        public DateOnly DataInicio { get; set; }
        public DateOnly DataFim { get; set; }
        public TimeOnly HoraInicial { get; set; }
        public TimeOnly HoraFinal { get; set; }
        public int IntervaloMinutos { get; set; }
    }
}
