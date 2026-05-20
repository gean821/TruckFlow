using TruckFlow.Domain.Dto.Conferencia;

namespace TruckFlow.Application.Interfaces
{
    public interface IConferenciaService
    {
        Task<ConferenciaResponseDto> GetByAgendamentoIdAsync(Guid agendamentoId, CancellationToken token = default);

        Task<ConferenciaItemDto> MatchItemAsync(Guid itemId, Guid produtoId, CancellationToken token = default);
    }
}