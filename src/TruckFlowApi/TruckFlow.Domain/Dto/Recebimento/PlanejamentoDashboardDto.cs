using System;
using System.Collections.Generic;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class PlanejamentoDashboardDto
    {
        public Guid Id { get; set; }
        public Guid FornecedorId { get; set; }
        public string FornecedorNome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DataReferencia { get; set; }

        public decimal TotalDiario { get; set; }
        public decimal TotalDiarioRecebido { get; set; }
        public decimal TotalDiarioRestante { get; set; }

        public decimal TotalPlanejado { get; set; }
        public decimal TotalRecebido { get; set; }
        public decimal TotalRestante { get; set; }

        public List<PlanejamentoDashboardItemDto> Itens { get; set; } = new();
    }

    public class PlanejamentoDashboardItemDto
    {
        public Guid Id { get; set; }
        public Guid ProdutoId { get; set; }
        public string Produto { get; set; } = string.Empty;

        public decimal CadenciaDiariaPlanejada { get; set; }
        public decimal ToleranciaExtra { get; set; }

        public decimal QuantidadeTotalPlanejada { get; set; }
        public decimal QuantidadeTotalRecebida { get; set; }
        public decimal FaltaReceber { get; set; }

        public decimal RecebidoNoDia { get; set; }
        public decimal FaltaNoDia { get; set; }

        public bool OperaNoDia { get; set; }
        public bool Congelado { get; set; }

        public string DiasSemana { get; set; } = string.Empty;
    }
}
