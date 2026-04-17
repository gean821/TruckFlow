using System;

namespace TruckFlow.Domain.Dto.ItensPlanejamento
{
    public class ItemPlanejamentoCreateDto
    {
        public required Guid ProdutoId { get; set; }
        public Guid? PlanejamentoRecebimentoId { get; set; }

        public Guid EmpresaId { get; set; }

        public required decimal CadenciaDiariaPlanejada { get; set; }

        public required string DiasSemana { get; set; } = string.Empty;

        public decimal? ToleranciaExtra { get; set; }
    }
}
