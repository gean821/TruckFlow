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
        public required string Fornecedor { get; set; } = string.Empty;
        public Guid ProdutoId { get; set; }
        public string UnidadeEntrega { get; set; } = string.Empty;
        public string LocalDescarga{ get; set; } = string.Empty;

        public required string Produto { get; set; } = string.Empty;
        public required Guid Id { get; set; }
        public required string DiasSemana { get; set; } = string.Empty;
        public required DateOnly DataInicio { get; set; }
        public required DateOnly DataFim { get; set; }
        public required TimeOnly HoraInicial { get; set; }
        public required TimeOnly HoraFinal { get; set; }
        public string? TempoDuracao { get; set; }
        public required int IntervaloMinutos { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
