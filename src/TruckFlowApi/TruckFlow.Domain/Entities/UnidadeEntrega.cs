 namespace TruckFlow.Application.Entities
{
    public class UnidadeEntrega : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Localizacao { get; set; }
        public Agendamento? Agendamento { get; set; }
    }
}
