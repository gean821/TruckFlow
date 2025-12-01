using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Grade
{
    public class GradeUpdateDto
    {
        public List<Guid>? FornecedorIds { get; set; }
        public List<Guid>? ProdutoIds { get; set; }
        public required DateOnly DataInicio { get; set; }
        public required DateOnly DataFim { get; set; }
        public required TimeOnly HoraInicial { get; set; }
        public required TimeOnly HoraFinal { get; set; }
        public required int IntervaloMinutos { get; set; }
    }
}
