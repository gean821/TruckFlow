using TruckFlowApi.Infra.Repositories;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class AgendamentoExpirationLockConcorrenciaTests : IntegrationTestBase
{
    public AgendamentoExpirationLockConcorrenciaTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task TryAdvisoryLockEmpresaAsync_SegundaInstancia_NaoAdquireEnquantoPrimeiraSeguraALock()
    {
        var empresaId = Guid.NewGuid();

        using var dbA = Fixture.CreateDbContext();
        var repoA = new AgendamentoRepositorio(dbA);
        await using var txA = await repoA.BeginTransactionAsync();

        var lockA = await repoA.TryAdvisoryLockEmpresaAsync(empresaId);
        Assert.True(lockA);

        using var dbB = Fixture.CreateDbContext();
        var repoB = new AgendamentoRepositorio(dbB);
        await using var txB = await repoB.BeginTransactionAsync();

        var lockB = await repoB.TryAdvisoryLockEmpresaAsync(empresaId);
        Assert.False(lockB);

        await txB.RollbackAsync();
        await txA.CommitAsync();

        using var dbC = Fixture.CreateDbContext();
        var repoC = new AgendamentoRepositorio(dbC);
        await using var txC = await repoC.BeginTransactionAsync();

        var lockC = await repoC.TryAdvisoryLockEmpresaAsync(empresaId);
        Assert.True(lockC);

        await txC.CommitAsync();
    }

    [Fact]
    public async Task TryAdvisoryLockEmpresaAsync_EmpresasDiferentes_NaoSeBloqueiamEntreSi()
    {
        var empresaA = Guid.NewGuid();
        var empresaB = Guid.NewGuid();

        using var dbA = Fixture.CreateDbContext();
        var repoA = new AgendamentoRepositorio(dbA);
        await using var txA = await repoA.BeginTransactionAsync();
        var lockA = await repoA.TryAdvisoryLockEmpresaAsync(empresaA);

        using var dbB = Fixture.CreateDbContext();
        var repoB = new AgendamentoRepositorio(dbB);
        await using var txB = await repoB.BeginTransactionAsync();
        var lockB = await repoB.TryAdvisoryLockEmpresaAsync(empresaB);

        Assert.True(lockA);
        Assert.True(lockB);

        await txA.CommitAsync();
        await txB.CommitAsync();
    }
}
