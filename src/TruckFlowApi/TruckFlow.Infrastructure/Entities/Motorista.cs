using System.Linq;

namespace TruckFlow.Infrastructure.Entities
{
    public class Motorista
    {
        public required Guid Id { get; set; }
        public required string Nome { get; set; }
        public required string Telefone { get; set; }
        public ICollection<Agendamento>? Agendamentos { get; set; }
        public Veiculo? Veiculo { get; set; }   
    }
}
