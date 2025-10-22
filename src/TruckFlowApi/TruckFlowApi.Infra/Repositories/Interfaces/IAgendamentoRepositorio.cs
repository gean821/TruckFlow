using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IAgendamentoRepositorio
    {
        public Task<Agendamento> AddAgendamento(Agendamento agendamento, CancellationToken token = default);
        public Task<Agendamento> GetById(Guid id, CancellationToken token = default);
        public Task<List<Agendamento>> GetAll(CancellationToken token = default);
        public Task<Agendamento> Update(Guid id, Agendamento Agendamento, CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
        public Task SaveChangesAsync(CancellationToken token = default);
    }
}