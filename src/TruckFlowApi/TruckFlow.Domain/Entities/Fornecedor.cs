namespace TruckFlow.Domain.Entities
{
    public class Fornecedor : EntidadeBase
    {
        public required string Nome { get; set; }
        public NotaFiscal? NotaFiscal { get; set; }

        public Agendamento? Agendamento { get; set; }
    }
}