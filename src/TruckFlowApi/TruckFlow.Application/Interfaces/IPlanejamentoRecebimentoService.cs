using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;

namespace TruckFlow.Application.Interfaces
{
    public interface IPlanejamentoRecebimentoService
    {
        Task<List<RecebimentoResponseDto>> GetAll(CancellationToken token = default);

        Task<PagedResponse<RecebimentoResponseDto>> GetPaged(
            PlanejamentoListQueryDto query,
            CancellationToken token = default);

        Task<RecebimentoResponseDto> GetById(Guid id, CancellationToken token = default);

        Task<RecebimentoResponseDto> CreateRecebimento(
            RecebimentoCreateDto recebimento,
            CancellationToken token = default);

        Task DeleteRecebimento(Guid id, CancellationToken token = default);

        Task<RecebimentoResponseDto> UpdateRecebimento(
            Guid id,
            RecebimentoUpdateDto recebimento,
            CancellationToken token = default);

        Task<PlanejamentoDashboardDto> GetDashboard(
            Guid id,
            DateTime? dataReferencia,
            CancellationToken token = default);

        Task<PlanejamentoRelatorioDto> GetRelatorio(
            Guid id,
            CancellationToken token = default);

        Task Encerrar(Guid id, CancellationToken token = default);
    }
}
