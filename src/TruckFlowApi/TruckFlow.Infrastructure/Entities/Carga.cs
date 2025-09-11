using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Infrastructure.Entities
{
    public class Carga
    {
        public required Guid Id { get; set; }
        public Guid AgendamentoId;
        public TipoCarga TipoCarga {get;set;}
    }
}


