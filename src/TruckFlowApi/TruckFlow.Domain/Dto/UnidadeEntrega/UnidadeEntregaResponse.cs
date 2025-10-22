using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlow.Domain.Dto.UnidadeEntrega
{
    public class UnidadeEntregaResponse
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
        public DateTime CreateAdt { get; set; }
    }
}
