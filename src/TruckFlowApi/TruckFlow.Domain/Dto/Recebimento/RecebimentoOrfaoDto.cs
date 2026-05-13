using System;

namespace TruckFlow.Domain.Dto.Recebimento
{
    public class RecebimentoOrfaoDto
    {
        public Guid Id { get; set; }
        public Guid? AgendamentoId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? ProdutoNome { get; set; }
        public Guid? FornecedorId { get; set; }
        public string? FornecedorNome { get; set; }
        public decimal Quantidade { get; set; }
        public DateTime DataRecebimento { get; set; }
        public string? Observacao { get; set; }
    }

    public class VincularOrfaoDto
    {
        public Guid ItemPlanejamentoId { get; set; }
    }
}
