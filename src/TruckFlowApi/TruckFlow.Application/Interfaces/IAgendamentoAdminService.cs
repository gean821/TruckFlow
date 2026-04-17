using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoAdminService
    {
        Task<AgendamentoAdminResponse> CreateAvulso(AgendamentoAdminCreateDto dto, CancellationToken token = default);
        Task<PagedResponse<AgendamentoAdminResponse>> GetByFiltros(AgendamentoFilterDto filtros, CancellationToken token = default);
        Task<AgendamentoAdminResponse> GetById(Guid id, CancellationToken token = default);

        Task<AgendamentoAdminResponse> Update(Guid id, AgendamentoAdminUpdateDto dto, CancellationToken token = default);

        Task RegistrarChegadaAsync(Guid agendamentoId, CancellationToken token = default);

        Task FinalizarAgendamento(
            Guid id,
            decimal quantidadeRecebida,
            CancellationToken token = default);

        Task FinalizarOperacao(Guid agendamentoId, CancellationToken token = default);

        Task CancelarAgendamento(Guid agendamentoId, CancellationToken token = default);

        Task Delete(Guid id, CancellationToken token = default);
    }
}
