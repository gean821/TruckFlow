using TruckFlow.Domain.Dto.RelatorioMotorista;

namespace TruckFlow.Application.Interfaces
{
    public interface IRelatorioMotoristaService
    {
        Task<RelatorioMotoristaResponseDto> GerarAsync(
            RelatorioMotoristaQueryDto query,
            CancellationToken token = default
        );
    }
}
