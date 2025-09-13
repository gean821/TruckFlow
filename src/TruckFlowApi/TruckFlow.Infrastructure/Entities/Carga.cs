using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Infrastructure.Entities
{
    public class Carga : EntidadeBase
    {
        public Guid AgendamentoId;
        public TipoCarga TipoCarga {get;set;}
    }
}


