using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.LocalDescarga
{
    public class LocalDescargaCreateDto
    {
        public required string Nome { get; set; }
        public required Guid UnidadeEntregaId { get; set; }
        public bool? Status { get; set; }
    }
}

