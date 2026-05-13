using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;
using TruckFlow.Domain.Enums;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IRecebimentoEventoRepositorio
    {
        Task<RecebimentoEvento> AddAsync(RecebimentoEvento evento, CancellationToken token = default);

        Task<RecebimentoEvento?> GetByAgendamentoId(Guid agendamentoId, CancellationToken token = default);

        Task<RecebimentoEvento?> GetByAgendamentoIdETipo(
            Guid agendamentoId,
            TipoMovimentoRecebimento tipo,
            CancellationToken token = default);

        Task<List<RecebimentoEvento>> GetByAgendamentoIdAll(
            Guid agendamentoId,
            CancellationToken token = default);

        Task<PagedResponse<RecebimentoEvento>> GetOrfaosPagedAsync(
            RecebimentoOrfaoQueryDto query,
            CancellationToken token = default);

        Task<RecebimentoEvento?> GetById(Guid id, CancellationToken token = default);

        Task Remove(RecebimentoEvento evento, CancellationToken token = default);

        Task SaveChangeAsync(CancellationToken token = default);
    }
}
