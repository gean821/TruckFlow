namespace TruckFlow.Domain.Dto.NotaFiscal
{
    public class NotaFiscalXmlItemExtraidoDto
    {
        public required string Codigo { get; init; }
        public string? Ean { get; init; }
        public required string Descricao { get; init; }
        public required decimal Quantidade { get; init; }
        public required string Unidade { get; init; }
        public required decimal ValorUnitario { get; init; }
        public required decimal ValorTotal { get; init; }
    }
}
