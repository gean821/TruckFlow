using TruckFlow.Application.Enums;

namespace TruckFlow.Application.Entities
{
    public class Administrador : EntidadeBase
    {
        public required string Nome { get; set; } 
        // public required string Email { get; set; }
        // public required string Password { get; set; }  retirado essas duas props pois elas veem do Usuario do identity.
        public required FuncaoAdministrador FuncaoAdm { get; set; }
        public required Usuario Usuario { get; set; }
        public required Guid UsuarioId { get; set; }
    }
}
