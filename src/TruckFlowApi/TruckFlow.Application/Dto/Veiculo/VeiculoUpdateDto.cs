﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Application.Dto.Veiculo
{
    public class VeiculoUpdateDto
    {
        public string? Nome { get; set; }
        public required string Placa { get; set; }
        public required Guid MotoristaId { get; set; }
        public required TipoVeiculo TipoVeiculo { get; set; }
    }
}
