using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class NotaFiscalItem : EntidadeBase, IEmpresaScoped
    {
        public Guid NotaFiscalId { get; set; }
        public required NotaFiscal NotaFiscal { get; set; }
        public required string Codigo { get; set; }
        public string? Ean { get; set; }
        public required string Descricao { get; set; }
        public decimal Quantidade { get; set; }
        public string? Unidade { get; set; }
        public required decimal ValorUnitario { get; set; }
        public required decimal ValorTotal { get; set; }

        public Guid? ProdutoId { get; set; }
        public Produto? Produto { get; set; }

        public NotaFiscalItemStatus Status { get; set; } = NotaFiscalItemStatus.PendenteRevisao;
        public OrigemMatchProduto? OrigemMatch { get; set; }
        public DateTime? MatchadoEm { get; set; }
        public Guid? MatchadoPor { get; set; }

        public Guid EmpresaId { get; set; }
        public Empresa? Empresa { get; set; }
    }
}
