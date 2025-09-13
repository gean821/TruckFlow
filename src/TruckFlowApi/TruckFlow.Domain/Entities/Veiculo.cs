using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Entities
{
    public class Veiculo : EntidadeBase
    {
        public string? Nome { get; set; }
        public required string Placa{ get; set; }
        public required TipoVeiculo TipoVeiculo{ get; set; }
        public required Motorista Motorista { get; set; }
        public required Guid MotoristaId { get; set; }
    }
}