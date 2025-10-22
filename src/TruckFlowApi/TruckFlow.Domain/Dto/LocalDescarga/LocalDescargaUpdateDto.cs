using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Dto.LocalDescarga
{
    public class LocalDescargaUpdateDto
    {
        public required string Nome { get; set; }
        public Guid? ProdutoId { get; set; }
    }
}

