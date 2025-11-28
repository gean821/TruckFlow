using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public sealed class PlanejamentoRecebimento : EntidadeBase
    {
        public required Fornecedor Fornecedor { get; set; }
        public required Guid FornecedorId { get; set; }
        public DateTime DataInicio { get; set; }
        public StatusRecebimento StatusRecebimento { get; set; } = StatusRecebimento.Indefinido;
        public ICollection<ItemPlanejamento> ItemPlanejamentos { get; set; } = [];
    }
}
