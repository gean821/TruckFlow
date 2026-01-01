namespace TruckFlow.Domain.Entities
{
    public class UnidadeEntrega : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
        public ICollection<Agendamento>? Agendamentos { get; set; } = [];
        public ICollection<Grade>? Grades { get; set; } = [];
    }
}
