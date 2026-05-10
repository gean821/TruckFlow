using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Entities;

namespace TruckFlowApi.Infra.Repositories.Interfaces
{
    public interface IAgendamentoRepositorio
    {
        public Task<Agendamento> AddAgendamento(Agendamento agendamento, CancellationToken token = default);

        public Task AddRangeAsync(List<Agendamento> agendamentos, CancellationToken token = default);

        public Task<List<Agendamento>> GetAvailableByProdutos(
            IReadOnlyCollection<Guid> produtoIds,
            Guid? fornecedorIdNota,
            DateTime dataInicio,
            DateTime dataFim,
            Guid empresaId,
            CancellationToken token = default
            );
        public Task<Agendamento> GetById(Guid id, CancellationToken token = default);
        public Task<List<Agendamento>> GetAll(CancellationToken token = default);

        Task<PagedResponse<AgendamentoAdminResponse>> GetAdminViewAsync(
                                                AgendamentoFilterDto filtros,
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

        Task<List<Agendamento>> GetExpiradosCandidatos(
            DateTime referenciaUtc,
            int batchSize,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retorna agendamentos ATIVOS na mesma doca cuja janela [DataInicio, DataFim) sobrepõe
        /// com [inicioUtc, fimUtc), independente do produto. Uma doca não pode receber dois
        /// caminhões no mesmo horário, mesmo que sejam do mesmo produto.
        /// "Ativos" = Disponivel | Pendente | Agendado | EmAndamento (Cancelado/Expirado/Finalizado não bloqueiam).
        /// Se <paramref name="excludeAgendamentoId"/> é informado, ele é ignorado (uso típico: edição).
        /// </summary>
        Task<List<Agendamento>> GetConflitosAsync(
            Guid localDescargaId,
            DateTime inicioUtc,
            DateTime fimUtc,
            Guid? excludeAgendamentoId,
            CancellationToken cancellationToken = default);
    }
}