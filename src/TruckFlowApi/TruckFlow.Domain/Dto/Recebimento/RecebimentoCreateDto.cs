using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoCreateDto
    {
        public required Guid FornecedorId { get; set; }
        public required ICollection<ItemPlanejamentoCreateDto>? ItensPlanejamento { get; set; }
        public required DateTime DataInicio { get; set; }
    }
}
