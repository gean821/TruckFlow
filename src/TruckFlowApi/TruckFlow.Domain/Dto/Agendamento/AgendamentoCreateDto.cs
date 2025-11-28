using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public sealed class AgendamentoCreateDto
    {
        public required string UnidadeDescarga { get; set; }
        public required string Fornecedor { get; set; }
        public required string UnidadeEntrega { get; set; }
        public required string VolumeCarga { get; set; }
        public required string Produto { get; set; }
        public required TipoCarga TipoCarga { get; set; }
    }
}
