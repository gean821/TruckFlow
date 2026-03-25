using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Grade
{
    public class GradeListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public Guid? ProdutoId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? LocalDescargaId { get; set; }
         
        public DateOnly? DataInicio { get; set; }
        public DateOnly? DataFim { get; set; }

        public string? Search { get; set; }
    }
}
