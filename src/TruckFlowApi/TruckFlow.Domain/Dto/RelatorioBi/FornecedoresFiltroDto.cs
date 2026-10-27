namespace TruckFlow.Domain.Dto.RelatorioBi
{
    public sealed class FornecedoresFiltroDto
    {
        public DateOnly? Inicio { get; set; }
        public DateOnly? Fim { get; set; }
        public Guid? UnidadeEntregaId { get; set; }
        public Guid? LocalDescargaId { get; set; }
        public Guid? FornecedorId { get; set; }
        public Guid? ProdutoId { get; set; }
        public string? Agrupamento { get; set; }

        public RelatorioBiFiltroDto ParaFiltroBi(bool comFornecedor = true) => new()
        {
            Inicio = Inicio,
            Fim = Fim,
            UnidadeEntregaId = UnidadeEntregaId,
            LocalDescargaId = LocalDescargaId,
            FornecedorId = comFornecedor ? FornecedorId : null,
            ProdutoId = ProdutoId,
        };
    }
}
