using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;

namespace TruckFlow.Application.Interfaces
{
    public interface IRecebimentoEventoService
    {
        Task RegistrarRecebimentoManual(
            Guid itemPlanejamentoId,
            decimal quantidade,
            string? observacao,
            CancellationToken token
        );

        Task<PagedResponse<RecebimentoOrfaoDto>> GetOrfaos(
            RecebimentoOrfaoQueryDto query,
            CancellationToken token = default);

        Task VincularOrfao(
            Guid eventoId,
            Guid itemPlanejamentoId,
            CancellationToken token = default);
    }
}
