using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Factories;
using TruckFlow.Application.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
//aqui vou precisar deixar assim até defiinir como vamos usar a notaFiscal no projeto..

namespace TruckFlow.Application
{
    public class AgendamentoService : IAgendamentoService
    {
        private readonly IAgendamentoRepositorio _repo;
        private readonly IValidator<AgendamentoCreateDto> _createValidator;
        private readonly IValidator<AgendamentoUpdateDto> _deleteValidator;
        private readonly AgendamentoFactory _agendamentoFactory;

        public AgendamentoService(
            IAgendamentoRepositorio repo,
            IValidator<AgendamentoCreateDto> createValidator,
            IValidator<AgendamentoUpdateDto> deleteValidator,
            AgendamentoFactory agendamentoFactory
            )
        {
            _repo = repo;
            _createValidator = createValidator;
            _deleteValidator = deleteValidator;
            _agendamentoFactory = agendamentoFactory;
        }

        public async Task<AgendamentoMotoristaResponse> AddAgendamento(
            AgendamentoCreateDto agendamento,
            CancellationToken token = default)
        {
            throw new NotImplementedException();  
        }

        public async Task Delete(Guid id, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public async Task<List<AgendamentoMotoristaResponse>> GetAll(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AgendamentoMotoristaResponse> GetById(Guid id, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public async Task<AgendamentoMotoristaResponse> Update(
            Guid id,
            AgendamentoUpdateDto agendamento,
            CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
