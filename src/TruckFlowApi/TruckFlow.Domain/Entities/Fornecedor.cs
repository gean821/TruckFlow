namespace TruckFlow.Domain.Entities
{
    public class Fornecedor : EntidadeBase
    {
        public required string Nome { get; set; }
        public required string Cnpj { get; set; }
        public ICollection<Produto> Produtos { get; set; } = [];
        public ICollection<PlanejamentoRecebimento>? Recebimentos { get; set; } = [];
        public NotaFiscal? NotaFiscal { get; set; }
        public ICollection<Agendamento>? Agendamentos { get; set; } = [];
    }
}