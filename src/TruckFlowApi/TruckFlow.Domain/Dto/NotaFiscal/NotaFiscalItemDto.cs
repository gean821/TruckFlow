using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.NotaFiscal
{
    public class NotaFiscalItemDto
    {
        public required string Codigo { get; set; }
        public required string Descricao { get; set; }
        public string? Ean { get; set; }

        public Guid? ProdutoSistemaId { get; set; }
        public string? ProdutoSistemaNome { get; set; }
        public decimal Quantidade { get; set; }
        public string? Unidade { get; set; }
        public required decimal ValorUnitario { get; set; }
        public required decimal ValorTotal { get; set; }

        public NotaFiscalItemStatus Status { get; set; } = NotaFiscalItemStatus.PendenteRevisao;
        public OrigemMatchProduto? OrigemMatch { get; set; }
    }
}