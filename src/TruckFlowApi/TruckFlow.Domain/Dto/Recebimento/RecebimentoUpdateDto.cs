using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoUpdateDto
    {
        public required Guid FornecedorId { get; set; }
        public required ICollection<ItemPlanejamentoEditDto>? ItensPlanejamento { get; set; }
        public required DateTime DataInicio { get; set; }
    }
}
