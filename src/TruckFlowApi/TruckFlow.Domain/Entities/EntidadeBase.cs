using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TruckFlow.Application.Entities
{
    public class EntidadeBase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt{ get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
