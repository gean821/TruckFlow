using TruckFlow._2_Domain.Enums;

namespace TruckFlow._2_Domain.Entities
{
    public class NotaFiscal
    {
        public required Guid Id { get; set; }
        public required string Numero { get; set; }
        public required Fornecedor Fornecedor{ get; set; }
        public required TipoCarga TipoCarga { get; set; }
    }
}