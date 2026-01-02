using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public class ReservarAgendamentoDto
    {
        public Guid AgendamentoId { get; set; }
        public required string NotaFiscalChaveAcesso{ get; set; }
        public Guid? UsuarioId { get; set; }
    }
}
