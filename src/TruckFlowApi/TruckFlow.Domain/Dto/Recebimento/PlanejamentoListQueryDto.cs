using System;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class PlanejamentoListQueryDto
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
        public StatusRecebimento? Status { get; set; }

        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public string? Search { get; set; }
    }
}
