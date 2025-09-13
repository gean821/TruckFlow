using System.Linq;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Infrastructure.Entities
{
    public class Motorista : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Telefone { get; set; }
        public ICollection<Agendamento>? Agendamentos { get; set; }
        public Veiculo? Veiculo { get; set; }
        public required Usuario Usuario { get; set; }
        public required Guid UsuarioId { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
