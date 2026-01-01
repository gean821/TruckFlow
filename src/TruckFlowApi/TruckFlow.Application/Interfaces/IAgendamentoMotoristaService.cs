using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoMotoristaService
    {
        public Task<AgendamentoMotoristaResponse> BookAppointment
            (
            Guid agendamentoId,
            string chaveAcesso,
            Guid usuarioId,
            CancellationToken token = default
            );

        public Task<List<AgendamentoMotoristaResponse>> GetAvailableAppointments(
            Guid FornecedorId,
            DateTime data,
            CancellationToken token = default
            );

        public Task<List<AgendamentoMotoristaResponse>> GetDriverAppointments
            (
                Guid motoristaId,
                CancellationToken token = default
            );
    }
}
