using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoResponseDto
    {
        public Guid Id { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<ItemPlanejamentoResponseDto> Itens { get; set; } = [];
    }
}
