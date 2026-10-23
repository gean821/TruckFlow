using System.Data;
using Microsoft.EntityFrameworkCore;

namespace TruckFlowApi.Infra.Database
{
    public static class MigrationLockRunner
    {
        private const string LockKey = "truckflow-migrations";

        public static void MigrateComLock(AppDbContext db, TimeSpan? tempoMaximoDeEspera = null)
        {
            var maxEspera = tempoMaximoDeEspera ?? TimeSpan.FromMinutes(2);
            var connection = db.Database.GetDbConnection();
            var precisaAbrir = connection.State != ConnectionState.Open;

            if (precisaAbrir)
            {
                connection.Open();
            }

            try
            {
                var inicio = DateTime.UtcNow;

                while (true)
                {
                    using var lockCommand = connection.CreateCommand();
                    lockCommand.CommandText = $"SELECT pg_try_advisory_lock(hashtext('{LockKey}'))";
                    var lockAdquirida = (bool)lockCommand.ExecuteScalar()!;

                    if (lockAdquirida)
                    {
                        break;
                    }

                    if (DateTime.UtcNow - inicio >= maxEspera)
                    {
                        throw new TimeoutException(
                            $"Não foi possível obter o lock de migrations (pg_advisory_lock) após {maxEspera.TotalSeconds:N0}s — " +
                            "outra instância pode estar travada aplicando migrations.");
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                db.Database.Migrate();
            }
            finally
            {
                using var unlockCommand = connection.CreateCommand();
                unlockCommand.CommandText = $"SELECT pg_advisory_unlock(hashtext('{LockKey}'))";
                unlockCommand.ExecuteScalar();

                if (precisaAbrir)
                {
                    connection.Close();
                }
            }
        }
    }
}
