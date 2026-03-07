using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RegistrarEntradaDto
    {
        public decimal Quantidade { get; set; }
        public string? Observacao { get; set; }
    }
}
