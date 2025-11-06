using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.ItensPlanejamento
{
    public class ItemPlanejamentoCreateDto
    {
        public required Guid ProdutoId { get; set; }

        public required Guid PlanejamentoRecebimentoId {get;set;}
        public required decimal QuantidadeTotalPlanejada { get; set; }
        public required decimal CadenciaDiariaPlanejada { get; set; }
    }
}
