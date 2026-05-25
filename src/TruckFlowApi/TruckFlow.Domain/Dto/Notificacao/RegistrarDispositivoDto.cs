using TruckFlow.Domain.Enums;

namespace TruckFlow.Domain.Dto.Notificacao
{
    public class RegistrarDispositivoDto
    {
        public required string ExpoPushToken { get; set; }
        public required PlataformaDispositivo Plataforma { get; set; }
        public string? AppVersion { get; set; }
    }
}