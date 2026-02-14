namespace TruckFlow.Domain.Entities
{
    public class RecebimentoEvento : EntidadeBase
    {
        protected RecebimentoEvento() { }

        public RecebimentoEvento(
            ItemPlanejamento item,
            decimal quantidade,
            Guid? agendamentoId,
            string? observacao
        )
        {
            ItemPlanejamento = item ?? throw new ArgumentNullException(nameof(item));
            ItemPlanejamentoId = item.Id;

            Quantidade = quantidade > 0
                ? quantidade
                : throw new ArgumentException("Quantidade inválida.");

            AgendamentoId = agendamentoId;
            Observacao = observacao;
            DataRecebimento = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid ItemPlanejamentoId { get; private set; }
        public ItemPlanejamento ItemPlanejamento { get; private set; } = null!;

        public Guid? AgendamentoId { get; private set; }
        public Agendamento? Agendamento { get; private set; }

        public decimal Quantidade { get; private set; }
        public DateTime DataRecebimento { get; private set; }
        public string? Observacao { get; private set; }
    }
}
