using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlow.Domain.Entities
{
    public class ItemPlanejamento : EntidadeBase
    {
        public Produto? Produto { get; set; }
        public Guid? ProdutoId { get; set; }
        public PlanejamentoRecebimento? PlanejamentoRecebimento {get;set;}
        public required Guid PlanejamentoRecebimentoId { get; set; }
        public required decimal QuantidadeTotalPlanejada { get; set; }
        public required decimal CadenciaDiariaPlanejada { get; set; }
        public decimal QuantidadeTotalRecebida { get; set; } //esse é o campo calculado que tem na tabela do front.
    }
}
