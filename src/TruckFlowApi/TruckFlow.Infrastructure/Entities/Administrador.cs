using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Domain.Entities
{
    public class Administrador
    {
        public Guid Id { get; set; }
        public required string Nome { get; set; } 
        public required FuncaoAdministrador FuncaoAdm { get; set; } 
    }
}
