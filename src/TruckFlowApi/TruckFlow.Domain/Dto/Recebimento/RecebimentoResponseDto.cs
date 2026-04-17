using System;
using System.Collections.Generic;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoResponseDto
    {
        public Guid Id { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        public Guid FornecedorId { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = string.Empty;

        public decimal TotalItens { get; set; } = 0;

        public List<ItemPlanejamentoResponseDto> Itens { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }
}
