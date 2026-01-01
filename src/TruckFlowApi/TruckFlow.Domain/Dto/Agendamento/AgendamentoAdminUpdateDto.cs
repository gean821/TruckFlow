using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public sealed class AgendamentoAdminUpdateDto
    {
        public required Guid FornecedorId { get; set; }
        public required Guid UnidadeEntregaId { get; set; }
        public required DateTime DataInicio { get; set; }
        public required DateTime DataFim { get; set; }
        public string? Status{get;set;}
        public required TipoCarga TipoCarga { get; set; }
        public Guid? MotoristaId { get; set; }
        public Guid? NotaFiscalId { get; set; }
        public decimal? VolumeCarga { get; set; }
    }
}
