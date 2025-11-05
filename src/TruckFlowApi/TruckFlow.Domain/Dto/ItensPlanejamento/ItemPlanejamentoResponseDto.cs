using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.ItensPlanejamento
{
    public class ItemPlanejamentoResponseDto
    {
        public required Guid Id { get; set; }
        public string Produto { get; set; } = string.Empty;
        public string Fornecedor { get; set; } = string.Empty;
        public decimal QuantidadeTotalPlanejada { get; set; }
        public decimal CadenciaDiariaPlanejada { get; set; }
        public decimal QuantidadeTotalRecebida { get; set; }

        public decimal FaltaReceber { get; set; } = 0;
    }
}
