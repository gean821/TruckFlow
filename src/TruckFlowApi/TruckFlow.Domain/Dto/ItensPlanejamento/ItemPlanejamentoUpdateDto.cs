using System;

namespace TruckFlow.Domain.Dto.ItensPlanejamento
{
    public class ItemPlanejamentoUpdateDto
    {
        public required Guid ProdutoId { get; set; }
        public required Guid PlanejamentoRecebimentoId { get; set; }

        public required decimal CadenciaDiariaPlanejada { get; set; }
        public required string DiasSemana { get; set; } = string.Empty;
        public decimal? ToleranciaExtra { get; set; }
    }
}
