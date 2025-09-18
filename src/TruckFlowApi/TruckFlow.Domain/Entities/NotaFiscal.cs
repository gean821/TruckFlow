using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class NotaFiscal : EntidadeBase
    {
        public required string Numero { get; set; }
        public required Fornecedor Fornecedor{ get; set; }
        public Guid FornecedorId { get; set; }
        public required TipoCarga TipoCarga { get; set; }
        public Agendamento? Agendamento { get; set; }
    }
}