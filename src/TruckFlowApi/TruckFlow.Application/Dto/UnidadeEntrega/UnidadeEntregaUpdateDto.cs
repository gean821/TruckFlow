using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Dto.UnidadeEntrega
{
    public class UnidadeEntregaUpdateDto
    {
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
    }
}
