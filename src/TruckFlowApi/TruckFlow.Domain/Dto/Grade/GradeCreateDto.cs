using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Grade
{
    public class GradeCreateDto
    {
        public Guid FornecedorId { get; set; }
        public Guid ProdutoId { get; set; }
        public Guid UnidadeEntregaId { get; set; }
        public required DateOnly DataInicio { get; set; }
        public required DateOnly DataFim { get; set; }
        public required TimeOnly HoraInicial { get; set; }
        public required TimeOnly HoraFinal { get; set; }
        public required int IntervaloMinutos { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
