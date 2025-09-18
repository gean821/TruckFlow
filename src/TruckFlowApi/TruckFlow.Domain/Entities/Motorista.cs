using System.Linq;

namespace TruckFlow.Domain.Entities
{
    public class Motorista : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Telefone { get; set; }
        public Veiculo? Veiculo { get; set; }
        public Guid? VeiculoId { get; set;}
        public required Usuario Usuario { get; set; }
        public required Guid UsuarioId { get; set; }
    }
}
