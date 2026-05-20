namespace TruckFlow.Domain.Dto.Conferencia
{
    public sealed class ConferenciaResponseDto
    {
        public required Guid AgendamentoId { get; set; }
        public Guid? NotaFiscalId { get; set; }
        public string? ChaveAcesso { get; set; }
        public string? FornecedorNome { get; set; }
        public Guid? FornecedorId { get; set; }
        public required List<ConferenciaItemDto> Itens { get; set; }

        public int TotalItens => Itens.Count;
        public int PendentesCount { get; set; }
        public int MatchedCount { get; set; }
    }
}
