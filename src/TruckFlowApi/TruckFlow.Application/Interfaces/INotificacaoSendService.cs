using TruckFlow.Domain.Dto.Notificacao;

namespace TruckFlow.Application.Interfaces
{
    public interface INotificacaoSendService
    {
        Task EnviarParaMotoristaAsync(EnviarParaMotoristaDto dto, CancellationToken ct);
        Task EnviarParaEmpresaAsync(EnviarParaEmpresaDto dto, CancellationToken ct);
    }
}