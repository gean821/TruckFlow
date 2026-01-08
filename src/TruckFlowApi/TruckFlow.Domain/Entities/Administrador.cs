using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class Administrador : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string UserName { get;set; }
        public required FuncaoAdministrador FuncaoAdm { get; set; }
        public required Usuario Usuario { get; set; }
        public required Guid UsuarioId { get; set; }
    }
}
