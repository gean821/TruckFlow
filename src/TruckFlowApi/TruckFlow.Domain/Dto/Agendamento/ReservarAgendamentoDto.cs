using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Agendamento
{
    public class ReservarAgendamentoDto
    {
        public Guid AgendamentoId { get; set; }
        public required string NotaFiscalChaveAcesso{ get; set; }
        public Guid UsuarioId { get; set; }
        public string? PlacaVeiculo { get; set; }
        public TipoVeiculo? TipoVeiculo { get; set; }
    }
}
