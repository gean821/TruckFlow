using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Domain.Entities
{
    public class EntidadeBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
