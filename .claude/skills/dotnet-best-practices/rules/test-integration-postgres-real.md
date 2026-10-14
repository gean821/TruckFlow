---
title: Teste de integração com Postgres real (Testcontainers), nunca mock de banco
impact: HIGH
impactDescription: pega o que só o Postgres faz — xmin, exclusion constraint, filtered unique, SKIP LOCKED, migrations
tags: testing, xunit, testcontainers, postgres, integration
---

## Teste de integração com Postgres real (Testcontainers), nunca mock de banco

Regra do projeto (CLAUDE.md, feedback Aurora): **sem mock** em teste de integração — migration de produção já quebrou com mock. InMemory/SQLite não têm `xmin`, `tstzrange`, `EXCLUDE USING gist`, índice filtrado nem `FOR UPDATE SKIP LOCKED`, que são exatamente as garantias críticas do sistema.

Infra pronta em `TruckFlow.Test/Integration/`:
- `DatabaseFixture` (`IAsyncLifetime`): sobe `postgres:16-alpine` via Testcontainers, roda `MigrateAsync()` (migrations reais), `CreateDbContext(empresaContext)` com interceptors de tenant e outbox, `ResetAsync()` com `TRUNCATE ... RESTART IDENTITY CASCADE`.
- `[Collection(IntegrationCollection.Name)]` + `IntegrationTestBase` (reset antes de cada teste).
- `ServiceFactory` monta services com repositórios reais e `NullLogger<T>.Instance`.
- `TestDataSeeder` com builders (`NovaEmpresa`, `NovoProduto`, `NovaVagaDisponivel`).
- Fakes **escritos à mão** para o que não é banco (`FakeCurrentUserService`), sem Moq.

**Incorreto:**

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("x").Options;
var repo = new Mock<IAgendamentoRepositorio>();
repo.Setup(r => r.Update(It.IsAny<Agendamento>(), default)).Returns(Task.CompletedTask); // nunca vai concorrer
```

**Correto:**

```csharp
[Collection(IntegrationCollection.Name)]
public class TransportadoraServiceTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Create_ComCnpjDuplicadoNaMesmaEmpresa_LancaBusinessException()
    {
        var empresaId = await SeedEmpresaAsync();
        var ctx = ServiceFactory.NewEmpresaContext();
        using var scope = ctx.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(ctx);
        var svc = ServiceFactory.CreateTransportadoraService(db, ctx,
            new FakeCurrentUserService { EmpresaId = empresaId });

        await svc.Create(new() { RazaoSocial = "A", Cnpj = "12345678000199" }, CancellationToken.None);

        await Assert.ThrowsAsync<BusinessException>(() =>
            svc.Create(new() { RazaoSocial = "B", Cnpj = "12345678000199" }, CancellationToken.None));
    }
}
```

Nomes: `Metodo_Cenario_Resultado` em PT. Asserts com `Assert.*` do xUnit. Teste unitário puro (sem banco) só para domínio/validators/helpers (`TruckFlow.Test/Domain`, `/Application`).

Lacunas conhecidas da fixture: não inclui `AuditSaveChangesInterceptor` nem `RealtimeNotificationInterceptor` — teste que depende de auditoria/NOTIFY precisa adicioná-los. Não há teste HTTP (WebApplicationFactory) — autorização por role não é coberta (ver `test-cross-tenant-mandatory`).

Referência: [Testcontainers for .NET — PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/) · [EF Core — Testing against your production database system](https://learn.microsoft.com/ef/core/testing/testing-with-the-database) · [Vladimir Khorikov — Unit Testing Principles, Practices, and Patterns (cap. 8-10)](https://www.manning.com/books/unit-testing)
