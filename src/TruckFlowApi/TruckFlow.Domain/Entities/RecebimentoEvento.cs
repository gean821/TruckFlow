using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class RecebimentoEvento : EntidadeBase, IEmpresaScoped
    {
        protected RecebimentoEvento() { }

        public RecebimentoEvento(
            ItemPlanejamento item,
            decimal quantidade,
            Guid? agendamentoId,
            string? observacao,
            Guid? empresaId,
            TipoMovimentoRecebimento tipo = TipoMovimentoRecebimento.Reserva
        )
        {
            ItemPlanejamento = item ?? throw new ArgumentNullException(nameof(item));
            ItemPlanejamentoId = item.Id;
            ProdutoId = item.ProdutoId;
            FornecedorId = item.PlanejamentoRecebimento?.FornecedorId;

            Quantidade = quantidade > 0
                ? quantidade
                : throw new ArgumentException("Quantidade inválida.");

            AgendamentoId = agendamentoId;
            Observacao = observacao;
            Tipo = tipo;
            EmpresaId = empresaId ?? throw new ArgumentNullException(nameof(empresaId));
            DataRecebimento = DateTime.UtcNow;
            CreatedAt = DateTime.UtcNow;
        }

        public static RecebimentoEvento CriarOrfao(
            Guid? produtoId,
            Guid? fornecedorId,
            decimal quantidade,
            Guid? agendamentoId,
            string? observacao,
            Guid? empresaId
        )
        {
            if (quantidade <= 0)
                throw new ArgumentException("Quantidade inválida.");

            return new RecebimentoEvento
            {
                ItemPlanejamentoId = null,
                ProdutoId = produtoId,
                FornecedorId = fornecedorId,
                Quantidade = quantidade,
                AgendamentoId = agendamentoId,
                Observacao = observacao,
                Tipo = TipoMovimentoRecebimento.Reserva,
                DataRecebimento = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                EmpresaId = empresaId ?? Guid.Empty
            };
        }

        public Guid? ItemPlanejamentoId { get; private set; }
        public ItemPlanejamento? ItemPlanejamento { get; private set; }

        public Guid? AgendamentoId { get; private set; }
        public Agendamento? Agendamento { get; private set; }

        public Guid? ProdutoId { get; private set; }
        public Produto? Produto { get; private set; }

        public Guid? FornecedorId { get; private set; }
        public Fornecedor? Fornecedor { get; private set; }

        public decimal Quantidade { get; private set; }
        public DateTime DataRecebimento { get; private set; }
        public string? Observacao { get; private set; }

        public TipoMovimentoRecebimento Tipo { get; private set; } = TipoMovimentoRecebimento.Reserva;

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }

        public bool EhOrfao => ItemPlanejamentoId is null;

        public void Vincular(ItemPlanejamento item)
        {
            if (item is null) throw new ArgumentNullException(nameof(item));
            if (!EhOrfao) throw new InvalidOperationException("Evento já está vinculado a um item.");

            ItemPlanejamento = item;
            ItemPlanejamentoId = item.Id;
            ProdutoId = item.ProdutoId;
            FornecedorId = item.PlanejamentoRecebimento?.FornecedorId ?? FornecedorId;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
