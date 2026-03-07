using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Grade
{
    public class GradeUpdateDto
    {
        public Guid? FornecedorId { get; set; }
        public Guid?  ProdutoId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public  DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }
        public TimeOnly? HoraInicial { get; set; }
        public TimeOnly? HoraFinal { get; set; }
        public int? IntervaloMinutos { get; set; }
        public string? DiasSemana { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
    }
}
