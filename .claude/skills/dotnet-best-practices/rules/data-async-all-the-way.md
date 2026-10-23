---
title: Async de ponta a ponta — nunca .Result, .Wait() ou GetAwaiter().GetResult()
impact: HIGH
impactDescription: evita thread pool starvation sob carga (21 fábricas, pico de reservas)
tags: async, threadpool, deadlock, ef-core
---

## Async de ponta a ponta — nunca .Result, .Wait() ou GetAwaiter().GetResult()

Bloquear em código assíncrono segura uma thread do pool enquanto o I/O acontece. Sob carga, o pool esgota, requests enfileiram e a API "trava" sem CPU alta — o sintoma clássico de *thread pool starvation*. Toda chamada que toca banco, HTTP ou disco é `async`/`await` com o `CancellationToken` propagado. Sem `async void` (exceto event handler), sem `Task.Run` para "tornar async" código que já é async.

**Incorreto:**

```csharp
// Program.cs
scope.ServiceProvider.SeedRolesAsync().GetAwaiter().GetResult();

var empresa = _repo.GetById(id, ct).Result;
public async void Processar() { ... }
```

**Correto:**

```csharp
// Program.cs — top-level statements aceitam await
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.SeedRolesAsync();
}

var empresa = await _repo.GetById(id, ct);
public async Task ProcessarAsync(CancellationToken ct) { ... }
```

Exceção legítima e única: biblioteca **só** síncrona (Zeus NFe) — ver `async-sync-library-wrap`. `ValueTask` só onde a medição justificar.

**No TruckFlow:** dívida em `Program.cs:165` (`SeedRolesAsync().GetAwaiter().GetResult()`), que vai sair junto com as migrations do startup (ADR-0004 item 2).

Referência: [David Fowler — Async Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md) · [Stephen Cleary — Async/Await Best Practices](https://learn.microsoft.com/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) · [Diagnosing thread pool starvation](https://learn.microsoft.com/dotnet/core/diagnostics/debug-threadpool-starvation)
