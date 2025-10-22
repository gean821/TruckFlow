using TruckFlow.Application.Enums;

namespace TruckFlow.Application.Entities
{
    public class Carga : EntidadeBase
    {
        public Agendamento? Agedamento { get; set; }
        public TipoCarga TipoCarga {get;set;}
    }
}


