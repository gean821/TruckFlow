using TruckFlow._2_Domain.Enums;

namespace TruckFlow._2_Domain.Entities
{
    public class Carga
    {
        public required Guid Id { get; set; }
        public Guid AgendamentoId;
        public TipoCarga TipoCarga {get;set;}
    }
}


