using Microsoft.EntityFrameworkCore.Storage;
using TruckFlow.Application.Interfaces;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Dto.Agendamento;
using TruckFlow.Domain.Dto.Recebimento;
using TruckFlow.Domain.Dto.Shared;
using TruckFlow.Domain.Dto.Relatorio;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Repositories.Interfaces;
using AgendamentoEntity = TruckFlow.Domain.Entities.Agendamento;
using NotaFiscalEntity = TruckFlow.Domain.Entities.NotaFiscal;

namespace TruckFlow.Test.Application.AgendamentoMotorista.Fakes;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public Guid? UserIdOrNull { get; set; }
    public Guid? EmpresaId { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMotorista { get; set; } = true;
    public Guid? MotoristaId { get; set; }
    public string? NomeAutor { get; set; }
}

internal sealed class FakeAgendamentoRepositorioParaMotorista : IAgendamentoRepositorio
{
    public List<AgendamentoEntity> AgendamentosDoMotorista { get; set; } = [];
    public Guid? MotoristaIdRecebido { get; private set; }

    public Task<List<AgendamentoEntity>> GetByMotoristaIdAcrossTenants(
        Guid motoristaId, CancellationToken token = default)
    {
        MotoristaIdRecebido = motoristaId;
        return Task.FromResult(AgendamentosDoMotorista);
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

    public Task<List<AgendamentoEntity>> GetByMotoristaId(Guid motoristaId, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity> Update(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task Delete(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AgendamentoEntity>> ExisteAgendamentoBloqueantePorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<AgendamentoEntity>> ExisteAgendamentoBloqueantePorGradeParaExclusao(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<AgendamentoEntity?> GetByIdWithFornecedor(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetExpiradosCandidatos(
        DateTime referenciaUtc, int batchSize, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<AgendamentoEntity>> GetConflitosAsync(
        Guid localDescargaId, DateTime inicioUtc, DateTime fimUtc, Guid? excludeAgendamentoId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> TryAdvisoryLockEmpresaAsync(Guid empresaId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task DeleteTodosPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task DeleteDisponiveisPorGrade(Guid gradeId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeNotaFiscalRepositorio : INotaFiscalRepositorio
{
    public Task<NotaFiscalEntity> SaveParsedNotaAsync(NotaFiscalEntity nota, CancellationToken token) =>
        throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken token) =>
        throw new NotImplementedException();

    public Task<NotaFiscalEntity?> ObterPorChaveAsync(string chaveAcesso, CancellationToken token) =>
        throw new NotImplementedException();

    public Task<NotaFiscalEntity?> ObterPorChaveAcrossTenantsAsync(string chaveAcesso, CancellationToken token) =>
        throw new NotImplementedException();

    public Task<Guid?> GetUltimoProdutoIdPorFornecedorECodigo(
        Guid fornecedorId, string codigoFornecedor, CancellationToken token) =>
        throw new NotImplementedException();
}

internal sealed class FakePlanejamentoRecebimentoRepositorio : IPlanejamentoRecebimentoRepositorio
{
    public Task<List<PlanejamentoRecebimento>> GetAll(CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PagedResponse<PlanejamentoRecebimento>> GetPagedAsync(
        PlanejamentoListQueryDto query, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PlanejamentoRecebimento> GetById(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PlanejamentoRecebimento?> GetByIdWithEventos(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PlanejamentoRecebimento> CreateRecebimento(
        PlanejamentoRecebimento recebimento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task DeleteRecebimento(Guid id, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PlanejamentoRecebimento> UpdateRecebimento(
        Guid id, PlanejamentoRecebimento recebimento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task SaveChangesAsync(CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task<PlanejamentoRecebimento?> GetPlanejamentoAtivoPorFornecedor(
        Guid fornecedorId, CancellationToken token = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeAgendamentoRecebimentoLifecycleService : IAgendamentoRecebimentoLifecycleService
{
    public Task AoReservarAsync(AgendamentoEntity agendamento, decimal pesoEstimado, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AoCancelarOuExpirarAsync(AgendamentoEntity agendamento, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AoFinalizarAsync(AgendamentoEntity agendamento, decimal quantidadeRealRecebida, CancellationToken token = default) =>
        throw new NotImplementedException();

    public Task AoAtualizarReservaAsync(AgendamentoEntity agendamento, decimal novoPesoEstimado, CancellationToken token = default) =>
        throw new NotImplementedException();
}

internal sealed class FakeEmpresaContext : IEmpresaContext
{
    public Guid EmpresaId => throw new NotImplementedException();
    public Guid? EmpresaIdOrNull => null;

    public IDisposable WithTenant(Guid empresaId) => throw new NotImplementedException();
}
