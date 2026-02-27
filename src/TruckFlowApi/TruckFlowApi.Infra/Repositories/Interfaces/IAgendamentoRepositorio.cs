using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IAgendamentoRepositorio
    {
        public Task<Agendamento> AddAgendamento(Agendamento agendamento, CancellationToken token = default);

        public Task AddRangeAsync(List<Agendamento> agendamentos, CancellationToken token = default);

        public Task<List<Agendamento>> GetAvailable(
            Guid fornecedorId,
            DateTime dataInicio,
            DateTime dataFim,
            CancellationToken token = default
            );
        public Task<Agendamento> GetById(Guid id, CancellationToken token = default);
        public Task<List<Agendamento>> GetAll(CancellationToken token = default);

        Task<List<AgendamentoAdminResponse>> GetAdminViewAsync(
                                                DateTime dataInicio,
                                                DateTime dataFim,
                                                Guid? fornecedorId,
                                                Guid? unidadeEntregaId,
                                                CancellationToken cancellationToken = default
            );

        public Task<List<Agendamento>> GetByMotoristaId(Guid motoristaId, CancellationToken token = default);
        public Task<Agendamento> Update(Agendamento Agendamento, CancellationToken token = default);
        public Task Delete(Agendamento agendamento, CancellationToken token = default);

        Task<bool> ExisteAgendamentoBloqueantePorGrade(
            Guid gradeId,
            CancellationToken cancellationToken = default);
        public Task SaveChangesAsync(CancellationToken token = default);
        Task<Agendamento?> GetByIdWithFornecedor(Guid id, CancellationToken token = default);
    }
}