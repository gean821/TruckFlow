using TruckFlow.Application.Enums;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Entities
{
    public class Carga : EntidadeBase
    {
        public Agendamento? Agedamento { get; set; }
        public TipoCarga TipoCarga {get;set;}
    }
}


