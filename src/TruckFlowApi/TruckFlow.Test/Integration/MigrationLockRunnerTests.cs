using Microsoft.EntityFrameworkCore;
using TruckFlowApi.Infra.Database;
using TruckFlow.Test.Integration.Fixtures;

namespace TruckFlow.Test.Integration;

[Collection(IntegrationCollection.Name)]
public class MigrationLockRunnerTests : IntegrationTestBase
{
    public MigrationLockRunnerTests(DatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void MigrateComLock_SemContencao_AplicaMigrationsNormalmente()
    {
        using var db = Fixture.CreateDbContext();

        MigrationLockRunner.MigrateComLock(db, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void MigrateComLock_LockSeguraPorOutraConexao_LancaTimeoutQuandoExcedeOLimite()
    {
        using var dbSegurandoOLock = Fixture.CreateDbContext();
        var connection = dbSegurandoOLock.Database.GetDbConnection();
        connection.Open();

        using var lockCommand = connection.CreateCommand();
        lockCommand.CommandText = "SELECT pg_advisory_lock(hashtext('truckflow-migrations'))";
        lockCommand.ExecuteScalar();

        try
        {
            using var dbTentandoMigrar = Fixture.CreateDbContext();

            var ex = Assert.Throws<TimeoutException>(() =>
                MigrationLockRunner.MigrateComLock(dbTentandoMigrar, TimeSpan.FromSeconds(2)));

            Assert.Contains("pg_advisory_lock", ex.Message);
        }
        finally
        {
            using var unlockCommand = connection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(hashtext('truckflow-migrations'))";
            unlockCommand.ExecuteScalar();
        }
    }

    [Fact]
    public async Task MigrateComLock_LockLiberadaAntesDoTimeout_ConsegueMigrarNormalmente()
    {
        using var dbSegurandoOLock = Fixture.CreateDbContext();
        var connection = dbSegurandoOLock.Database.GetDbConnection();
        connection.Open();

        using (var lockCommand = connection.CreateCommand())
        {
            lockCommand.CommandText = "SELECT pg_advisory_lock(hashtext('truckflow-migrations'))";
            lockCommand.ExecuteScalar();
        }

        var liberarLock = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            using var unlockCommand = connection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(hashtext('truckflow-migrations'))";
            unlockCommand.ExecuteScalar();
        });

        using var dbTentandoMigrar = Fixture.CreateDbContext();

        MigrationLockRunner.MigrateComLock(dbTentandoMigrar, TimeSpan.FromSeconds(10));

        await liberarLock;
    }
}
