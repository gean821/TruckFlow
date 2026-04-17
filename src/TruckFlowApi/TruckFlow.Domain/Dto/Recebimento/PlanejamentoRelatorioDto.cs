using System;
using System.Collections.Generic;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class PlanejamentoRelatorioDto
    {
        public Guid Id { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = string.Empty;

        public decimal TotalPlanejado { get; set; }
        public decimal TotalRecebido { get; set; }
        public decimal TotalRestante { get; set; }
        public decimal PercentualAtingido { get; set; }

        public List<PlanejamentoRelatorioItemDto> Itens { get; set; } = new();
        public List<PlanejamentoRelatorioEventoDto> Eventos { get; set; } = new();
    }

    public class PlanejamentoRelatorioItemDto
    {
        public Guid Id { get; set; }
        public string Produto { get; set; } = string.Empty;
        public decimal QuantidadeTotalPlanejada { get; set; }
        public decimal QuantidadeTotalRecebida { get; set; }
        public decimal FaltaReceber { get; set; }
        public decimal PercentualAtingido { get; set; }
        public string DiasSemana { get; set; } = string.Empty;
    }

    public class PlanejamentoRelatorioEventoDto
    {
        public Guid Id { get; set; }
        public Guid? AgendamentoId { get; set; }
        public string Produto { get; set; } = string.Empty;
        public DateTime DataRecebimento { get; set; }
        public decimal Quantidade { get; set; }
        public string? Observacao { get; set; }
    }
}
