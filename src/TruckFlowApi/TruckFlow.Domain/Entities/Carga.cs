

using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class Carga : EntidadeBase
    {
        public Agendamento? Agedamento { get; set; }
        public TipoCarga TipoCarga {get;set;}
    }
}


