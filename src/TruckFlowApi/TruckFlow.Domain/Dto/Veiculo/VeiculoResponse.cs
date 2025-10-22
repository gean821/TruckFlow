using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Enums;

namespace TruckFlow.Domain.Dto.Veiculo
{
    public class VeiculoResponse
    {
        public required Guid Id { get; set; }
        public string? Nome { get; set; }
        public required string Placa { get; set; }
        public required TipoVeiculo TipoVeiculo { get; set; }
        public required string Motorista { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
