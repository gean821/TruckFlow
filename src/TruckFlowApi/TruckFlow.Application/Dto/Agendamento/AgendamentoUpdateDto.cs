using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Dto.Agendamento
{
    public sealed class AgendamentoUpdateDto
    {
        public required string UnidadeDescarga { get; set; }
        public required string Fornecedor { get; set; }

        public required string PesoCarga { get; set; }
        public required string Produto { get; set; }
    }
}
