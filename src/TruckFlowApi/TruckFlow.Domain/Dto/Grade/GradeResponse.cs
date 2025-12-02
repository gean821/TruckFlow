using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Fornecedor;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Domain.Dto.Grade
{
    public class GradeResponse
    {
        public required Guid FornecedorId { get; set; }
        public required string Fornecedor { get; set; }
        public Guid ProdutoId { get; set; }
        public required string Produto { get; set; }
        public required Guid Id { get; set; }
        public required DateOnly DataInicio { get; set; }
        public required DateOnly DataFim { get; set; }
        public required TimeOnly HoraInicial { get; set; }
        public required TimeOnly HoraFinal { get; set; }
        public required int IntervaloMinutos { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
