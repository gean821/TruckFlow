﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Entities;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Application.Interfaces
{
    public interface IAgendamentoService 
    {
        public Task<AgendamentoMotoristaResponse> AddAgendamento(AgendamentoCreateDto agendamento, CancellationToken token = default);
        public Task<AgendamentoMotoristaResponse> GetById(Guid id, CancellationToken token = default);
        public Task<List<AgendamentoMotoristaResponse>> GetAll(CancellationToken token = default);
        public Task<AgendamentoMotoristaResponse> Update(Guid id, AgendamentoUpdateDto agendamento, CancellationToken token = default);
        public Task Delete(Guid id, CancellationToken token = default);
    }
}
