using System;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRecebimentoEventoRepositorio
    {
        Task<RecebimentoEvento> AddAsync(RecebimentoEvento evento, CancellationToken token = default);

        Task<RecebimentoEvento?> GetByAgendamentoId(Guid agendamentoId, CancellationToken token = default);

        Task Remove(RecebimentoEvento evento, CancellationToken token = default);

        Task SaveChangeAsync(CancellationToken token = default);
    }
}
