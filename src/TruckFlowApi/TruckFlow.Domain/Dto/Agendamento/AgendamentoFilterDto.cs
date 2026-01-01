using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public class AgendamentoFilterDto
    {
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public Guid? FornecedorId { get; set; } 
        public Guid? UnidadeEntregaId { get; set; } 
    }
}
