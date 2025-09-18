using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Dto.Veiculo
{
    public class VeiculoCreateDto
    {
        public string? Nome { get; set; }
        public required string Placa { get; set; }
        public required TipoVeiculo TipoVeiculo { get; set; }
        public required Guid MotoristaId { get; set; }
    }
}
