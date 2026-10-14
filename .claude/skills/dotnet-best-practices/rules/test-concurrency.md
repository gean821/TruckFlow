---
title: Recurso disputado tem teste com N tarefas concorrentes, cada uma com seu DbContext
impact: HIGH
impactDescription: prova "exatamente 1 vencedor" em vez de assumir
tags: testing, concurrency, integration, xmin, skip-locked
---

## Recurso disputado tem teste com N tarefas concorrentes, cada uma com seu DbContext

Concorrência não se prova com teste sequencial. O padrão do projeto: N `Task.Run`, **cada uma** com `EmpresaContext`, `DbContext` e `FakeCurrentUserService` próprios (DbContext não é thread-safe — compartilhar invalida o teste), disparadas juntas, `Task.WhenAll`, e asserts sobre o resultado agregado **e** sobre o estado final no banco (lido com contexto novo).

**Incorreto:**

```csharp
await svc.BookAppointment(dto1);
await Assert.ThrowsAsync<BusinessException>(() => svc.BookAppointment(dto2)); // sequencial: testa o if, não a corrida
```

**Correto (padrão `AgendamentoReservaConcorrenciaTests`):**

```csharp
var tentativas = motoristaIds.Select(motoristaId => Task.Run(async () =>
{
    var empresaContext = ServiceFactory.NewEmpresaContext();
    using var tenantScope = empresaContext.WithTenant(empresaId);
    using var db = Fixture.CreateDbContext(empresaContext);
    var currentUser = new FakeCurrentUserService { UserId = motoristaId, EmpresaId = empresaId, IsMotorista = true };
    var svc = ServiceFactory.CreateAgendamentoMotoristaService(db, empresaContext, currentUser);
    try { await svc.BookAppointment(new ReservarAgendamentoDto { AgendamentoId = vagaId, NotaFiscalChaveAcesso = chave }); return true; }
    catch (Exception ex) when (ex is BusinessException or DomainException or DbUpdateConcurrencyException) { return false; }
})).ToArray();

var resultados = await Task.WhenAll(tentativas);
Assert.Single(resultados, r => r);
Assert.Equal(totalThreads - 1, resultados.Count(r => !r));

await using var assertDb = Fixture.CreateDbContext();
var vagaFinal = await assertDb.Agendamento.IgnoreQueryFilters().SingleAsync(a => a.Id == vagaId);
Assert.Equal(StatusAgendamento.Agendado, vagaFinal.StatusAgendamento);
```

Para workers: dois "workers" consumindo a mesma fila → cada item processado **uma** vez (`OutboxDispatcherConcorrenciaTests`). Capture só as exceções esperadas no `catch` — qualquer outra deve falhar o teste.

**No TruckFlow:** `TruckFlow.Test/Integration/AgendamentoReservaConcorrenciaTests.cs`, `OutboxDispatcherConcorrenciaTests.cs`. Load test de sistema fica em `TruckFlow.LoadTests` (NBomber) — complementar, não substituto.

Referência: [EF Core — DbContext thread safety](https://learn.microsoft.com/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues) · [xUnit — Shared context (collection fixtures)](https://xunit.net/docs/shared-context)
