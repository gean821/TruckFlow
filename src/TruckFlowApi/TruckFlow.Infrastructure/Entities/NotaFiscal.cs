using TruckFlow.Domain.Entities;
using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Infrastructure.Entities
{
    public class NotaFiscal : EntidadeBase
    {
        public required string Numero { get; set; }
        public required Fornecedor Fornecedor{ get; set; }
        public required TipoCarga TipoCarga { get; set; }
        public required DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}