using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
