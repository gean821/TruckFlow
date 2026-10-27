using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Relatorio
{
    public class RelatorioAgendamentoFilterDto
    {
        public DateTime? DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        public TimeSpan? HoraInicio { get; set; }
        public TimeSpan? HoraFim { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? PlacaVeiculo { get; set; }
        public string? Motorista {  get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public List<StatusAgendamento>? Status { get; set; }
        public OrigemAgendamento? Origem { get; set; }
        public SituacaoChegada? Chegada { get; set; }
        public string? Busca { get; set; }
        public string? OrdenarPor { get; set; }
        public bool Desc { get; set; }
    }
}
