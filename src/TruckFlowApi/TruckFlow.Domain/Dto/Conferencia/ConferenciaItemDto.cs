using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Conferencia
{
    public sealed class ConferenciaItemDto
    {
        public required Guid Id { get; set; }
        public required string Codigo { get; set; }
        public string? Ean { get; set; }
        public required string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public string? Unidade { get; set; }
        public required NotaFiscalItemStatus Status { get; set; }
        public OrigemMatchProduto? OrigemMatch { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? ProdutoNome { get; set; }
        public DateTime? MatchadoEm { get; set; }
        public Guid? MatchadoPor { get; set; }

        /// <summary>
        /// Top-3 produtos do catálogo por similaridade textual com a descrição da nota.
        /// Preenchido apenas quando Status = PendenteRevisao. Score em [0, 1].
        /// </summary>
        public List<ProdutoSugestaoDto>? Sugestoes { get; set; }
    }
}
