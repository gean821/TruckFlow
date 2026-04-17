using System;
using System.Collections.Generic;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoUpdateDto
    {
        public required Guid FornecedorId { get; set; }
        public required ICollection<ItemPlanejamentoUpdateDto>? ItensPlanejamento { get; set; }
        public required DateTime DataInicio { get; set; }
        public required DateTime DataFim { get; set; }

        public Guid EmpresaId { get; set; }
    }
}
