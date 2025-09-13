namespace TruckFlow.Domain.Entities
{
    public class Notificacao : EntidadeBase
    {
        public required string Descricao { get; set; }
        public Agendamento? Agendamento { get; set; }
        public Guid? AgendamentoId { get; set; }
    }
}