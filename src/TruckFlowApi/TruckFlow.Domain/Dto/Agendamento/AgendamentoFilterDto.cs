using System;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public class AgendamentoFilterDto
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }

        public Guid? FornecedorId { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? ProdutoId { get; set; }

        public StatusAgendamento? Status { get; set; }
        public TipoVeiculo? TipoVeiculo { get; set; }

        public string? Search { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
