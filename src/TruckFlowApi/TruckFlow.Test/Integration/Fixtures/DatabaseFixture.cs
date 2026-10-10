using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using TruckFlow.Domain.Contracts;
using TruckFlow.Domain.Entities;
using TruckFlowApi.Infra.Database;
using TruckFlowApi.Infra.Database.Interceptors;

namespace TruckFlow.Test.Integration.Fixtures;

public sealed class DatabaseFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("truckflow_test")
            .WithUsername("truckflow_test")
            .WithPassword("truckflow_test")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public AppDbContext CreateDbContext(IEmpresaContext? empresaContext = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(
                new EmpresaScopedSaveChangesInterceptor(empresaContext ?? new NullEmpresaContext()),
                new OutboxSaveChangesInterceptor())
            .Options;

        return new AppDbContext(options, empresaContext ?? new NullEmpresaContext());
    }

    public async Task ResetAsync()
    {
        using var db = CreateDbContext();
        var tableNames = db.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct()
            .ToList();

        var tables = string.Join(", ", tableNames.Select(n => $"\"{n}\""));
        var sql = "TRUNCATE TABLE " + tables + " RESTART IDENTITY CASCADE;";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    private sealed class NullEmpresaContext : IEmpresaContext
    {
        public Guid EmpresaId => throw new UnauthorizedAccessException("Tenant não identificado.");
        public Guid? EmpresaIdOrNull => null;
        public IDisposable WithTenant(Guid empresaId) => new NoOpScope();

        private sealed class NoOpScope : IDisposable
        {
            public void Dispose() { }
        }
    }
}
