using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlowApi.Infra.Repositories.Interfaces;
using AgendamentoEntity = TruckFlow.Domain.Entities.Agendamento;

namespace TruckFlow.Test.Application.AgendamentoExpiracao.Fakes;

internal sealed class FakeAgendamentoRepositorioParaExpiracao : IAgendamentoRepositorio
{
    public List<AgendamentoEntity> Candidatos { get; set; } = [];
    public bool LockAdquirido { get; set; } = true;
    public bool LancarConcurrencyExceptionNoSave { get; set; }

    public Guid? EmpresaIdRecebidaNaLock { get; private set; }
    public bool TransacaoFoiAberta { get; private set; }
    public bool SaveChangesFoiChamado { get; private set; }

    public FakeDbContextTransaction? UltimaTransacao { get; private set; }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        TransacaoFoiAberta = true;
        UltimaTransacao = new FakeDbContextTransaction();
        return Task.FromResult<IDbContextTransaction>(UltimaTransacao);
    }

    public Task<bool> TryAdvisoryLockEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default)
    {
        EmpresaIdRecebidaNaLock = empresaId;
        return Task.FromResult(LockAdquirido);
    }

    public Task<List<AgendamentoEntity>> GetExpiradosCandidatos(
        DateTime referenciaUtc,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Candidatos);

    public Task SaveChangesAsync(CancellationToken token = default)
    {
        SaveChangesFoiChamado = true;

        if (LancarConcurrencyExceptionNoSave)
        {
            throw new DbUpdateConcurrencyException("Conflito de concorrência simulado.");
        }

        return Task.CompletedTask;
    }

    public Task<AgendamentoEntity> AddAgendamento(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AddRangeAsync(List<AgendamentoEntity> agendamentos, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetAvailableByProdutos(
        IReadOnlyCollection<Guid> produtoIds, Guid? fornecedorIdNota, DateTime dataInicio, DateTime dataFim,
        Guid empresaId, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity> GetById(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity?> GetByIdAcrossTenants(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetAll(CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PagedResponse<AgendamentoAdminResponse>> GetAdminViewAsync(
        AgendamentoFilterDto filtros, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<int> CountLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoRelatorioLinha>> GetLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int limite, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public IAsyncEnumerable<AgendamentoRelatorioLinha> StreamLinhasRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int limite) =>
        throw new NotImplementedException();

    public Task<TruckFlow.Domain.Dto.Shared.PagedResponse<AgendamentoRelatorioLinha>> GetPaginaRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<RelatorioResumoLinha>> ListarResumoRelatorioAsync(
        RelatorioAgendamentoCriteria criteria, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<FimDescargaDoca>> ListarFinsDescargaAsync(
        DateTime deUtc, DateTime ateUtc, IReadOnlyCollection<Guid>? docas = null, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetByMotoristaId(Guid motoristaId, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetByMotoristaIdAcrossTenants(Guid motoristaId, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<TruckFlow.Domain.Dto.RelatorioMotorista.RelatorioMotoristaLinha>> ListarParaRelatorioMotoristaAcrossTenants(
        Guid motoristaId, DateTime desdeUtc, DateTime ateUtc, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<TruckFlow.Domain.Dto.RelatorioMotorista.RelatorioMotoristaProximoDto?> ObterProximoAgendamentoMotoristaAcrossTenants(
        Guid motoristaId, DateTime agoraUtc, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity> Update(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task Delete(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AgendamentoEntity>> ExisteAgendamentoBloqueantePorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AgendamentoEntity>> ExisteAgendamentoBloqueantePorGradeParaExclusao(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity?> GetByIdWithFornecedor(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetConflitosAsync(
        Guid localDescargaId, DateTime inicioUtc, DateTime fimUtc, Guid? excludeAgendamentoId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task DeleteTodosPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task DeleteDisponiveisPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();
    public bool Comitada { get; private set; }
    public bool Revertida { get; private set; }

    public void Commit() => Comitada = true;
    public void Rollback() => Revertida = true;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Comitada = true;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        Revertida = true;
        return Task.CompletedTask;
    }

    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class FakeAgendamentoRecebimentoLifecycleService : IAgendamentoRecebimentoLifecycleService
{
    public bool AoCancelarOuExpirarFoiChamado { get; private set; }
    public int ChamadasDeEstorno { get; private set; }
    public bool LancarExceptionNoEstorno { get; set; }

    public Task AoReservarAsync(AgendamentoEntity agendamento, decimal pesoEstimado, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AoCancelarOuExpirarAsync(AgendamentoEntity agendamento, CancellationToken token = default)
    {
        AoCancelarOuExpirarFoiChamado = true;
        ChamadasDeEstorno++;

        if (LancarExceptionNoEstorno)
        {
            throw new InvalidOperationException("Falha simulada no estorno.");
        }

        return Task.CompletedTask;
    }

    public Task AoFinalizarAsync(AgendamentoEntity agendamento, decimal quantidadeRealRecebida, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AoAtualizarReservaAsync(AgendamentoEntity agendamento, decimal novoPesoEstimado, CancellationToken token = default) =>
        throw new NotImplementedException();
}
