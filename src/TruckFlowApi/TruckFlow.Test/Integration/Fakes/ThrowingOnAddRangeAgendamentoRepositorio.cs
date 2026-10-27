using Microsoft.EntityFrameworkCore.Storage;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;

namespace TruckFlow.Test.Integration.Fakes;

internal sealed class ThrowingOnAddRangeAgendamentoRepositorio(IAgendamentoRepositorio inner) : IAgendamentoRepositorio
{
    public Task AddRangeAsync(List<Agendamento> agendamentos, CancellationToken token = default) =>
        throw new InvalidOperationException("Falha simulada ao inserir os novos slots (teste de rollback).");

    public Task<Agendamento> AddAgendamento(Agendamento agendamento, CancellationToken token = default) =>
        inner.AddAgendamento(agendamento, token);

    public Task<List<Agendamento>> GetAvailableByProdutos(
        IReadOnlyCollection<Guid> produtoIds, Guid? fornecedorIdNota, DateTime dataInicio, DateTime dataFim,
        Guid empresaId, CancellationToken token = default) =>
        inner.GetAvailableByProdutos(produtoIds, fornecedorIdNota, dataInicio, dataFim, empresaId, token);

    public Task<Agendamento> GetById(Guid id, CancellationToken token = default) =>
        inner.GetById(id, token);

    public Task<Agendamento?> GetByIdAcrossTenants(Guid id, CancellationToken token = default) =>
        inner.GetByIdAcrossTenants(id, token);

    public Task<List<Agendamento>> GetAll(CancellationToken token = default) =>
        inner.GetAll(token);

    public Task<PagedResponse<AgendamentoAdminResponse>> GetAdminViewAsync(
        AgendamentoFilterDto filtros, CancellationToken cancellationToken = default) =>
        inner.GetAdminViewAsync(filtros, cancellationToken);

    public Task<int> CountLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, CancellationToken cancellationToken = default) =>
        inner.CountLinhasRelatorioAsync(criteria, cancellationToken);

    public Task<List<AgendamentoRelatorioLinha>> GetLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int limite, CancellationToken cancellationToken = default) =>
        inner.GetLinhasRelatorioAsync(criteria, limite, cancellationToken);

    public IAsyncEnumerable<AgendamentoRelatorioLinha> StreamLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int limite) =>
        inner.StreamLinhasRelatorioAsync(criteria, limite);

    public Task<PagedResponse<AgendamentoRelatorioLinha>> GetPaginaRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        inner.GetPaginaRelatorioAsync(criteria, pageNumber, pageSize, cancellationToken);

    public Task<List<RelatorioResumoLinha>> ListarResumoRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, CancellationToken cancellationToken = default) =>
        inner.ListarResumoRelatorioAsync(criteria, cancellationToken);

    public Task<List<FimDescargaDoca>> ListarFinsDescargaAsync(
        DateTime deUtc, DateTime ateUtc, IReadOnlyCollection<Guid>? docas = null, CancellationToken cancellationToken = default) =>
        inner.ListarFinsDescargaAsync(deUtc, ateUtc, docas, cancellationToken);

    public Task<List<Agendamento>> GetByMotoristaId(Guid motoristaId, CancellationToken token = default) =>
        inner.GetByMotoristaId(motoristaId, token);

    public Task<List<Agendamento>> GetByMotoristaIdAcrossTenants(Guid motoristaId, CancellationToken token = default) =>
        inner.GetByMotoristaIdAcrossTenants(motoristaId, token);

    public Task<List<TruckFlow.Domain.Dto.RelatorioMotorista.RelatorioMotoristaLinha>> ListarParaRelatorioMotoristaAcrossTenants(
        Guid motoristaId, DateTime desdeUtc, DateTime ateUtc, CancellationToken token = default) =>
        inner.ListarParaRelatorioMotoristaAcrossTenants(motoristaId, desdeUtc, ateUtc, token);

    public Task<TruckFlow.Domain.Dto.RelatorioMotorista.RelatorioMotoristaProximoDto?> ObterProximoAgendamentoMotoristaAcrossTenants(
        Guid motoristaId, DateTime agoraUtc, CancellationToken token = default) =>
        inner.ObterProximoAgendamentoMotoristaAcrossTenants(motoristaId, agoraUtc, token);

    public Task<Agendamento> Update(Agendamento agendamento, CancellationToken token = default) =>
        inner.Update(agendamento, token);

    public Task Delete(Agendamento agendamento, CancellationToken token = default) =>
        inner.Delete(agendamento, token);

    public Task<IReadOnlyList<Agendamento>> ExisteAgendamentoBloqueantePorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        inner.ExisteAgendamentoBloqueantePorGrade(gradeId, cancellationToken);

    public Task<IReadOnlyList<Agendamento>> ExisteAgendamentoBloqueantePorGradeParaExclusao(Guid gradeId, CancellationToken cancellationToken = default) =>
        inner.ExisteAgendamentoBloqueantePorGradeParaExclusao(gradeId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken token = default) =>
        inner.SaveChangesAsync(token);

    public Task<Agendamento?> GetByIdWithFornecedor(Guid id, CancellationToken token = default) =>
        inner.GetByIdWithFornecedor(id, token);

    public Task<List<Agendamento>> GetExpiradosCandidatos(
        DateTime referenciaUtc, int batchSize, CancellationToken cancellationToken = default) =>
        inner.GetExpiradosCandidatos(referenciaUtc, batchSize, cancellationToken);

    public Task<List<Agendamento>> GetConflitosAsync(
        Guid localDescargaId, DateTime inicioUtc, DateTime fimUtc, Guid? excludeAgendamentoId,
        CancellationToken cancellationToken = default) =>
        inner.GetConflitosAsync(localDescargaId, inicioUtc, fimUtc, excludeAgendamentoId, cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        inner.BeginTransactionAsync(cancellationToken);

    public Task<bool> TryAdvisoryLockEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
        inner.TryAdvisoryLockEmpresaAsync(empresaId, cancellationToken);

    public Task DeleteTodosPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        inner.DeleteTodosPorGrade(gradeId, cancellationToken);

    public Task DeleteDisponiveisPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        inner.DeleteDisponiveisPorGrade(gradeId, cancellationToken);
}
