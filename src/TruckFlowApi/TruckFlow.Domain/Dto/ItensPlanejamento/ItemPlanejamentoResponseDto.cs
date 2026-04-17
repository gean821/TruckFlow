using System;

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

        public decimal FaltaReceber { get; set; }

        public string DiasSemana { get; set; } = string.Empty;
        public decimal ToleranciaExtra { get; set; }

        public required DateTime CreatedAt { get; set; }
    }
}
