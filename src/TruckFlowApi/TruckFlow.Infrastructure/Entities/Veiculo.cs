using TruckFlow.Infrastructure.Enums;

namespace TruckFlow.Infrastructure.Entities
{
    public class Veiculo
    {
        public required Guid Id { get; set; }
        public string? Nome { get; set; }
        public required string Placa{ get; set; }
        public required TipoVeiculo TipoVeiculo{ get; set; }
        public required Motorista Motorista { get; set; }
    }
}