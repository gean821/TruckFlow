---
title: Lifetimes coerentes — Scoped para o que toca DbContext, nunca captive dependency
impact: MEDIUM
impactDescription: elimina DbContext compartilhado entre requests e estado de tenant vazando
tags: dependency-injection, lifetimes, scoped, singleton, dbcontext
---

## Lifetimes coerentes — Scoped para o que toca DbContext, nunca captive dependency

`AppDbContext` e `IEmpresaContext` são **Scoped** (um por request). Regras:

| Tipo | Lifetime |
|---|---|
| Services e repositórios (dependem de DbContext) | **Scoped** em código novo |
| Validators FluentValidation | Scoped (ou Singleton se sem dependências) |
| `CurrentUserGuard`, `ICurrentUserService`, `IEmpresaContext` | Scoped |
| Factories estáticas | não registra |
| Stateless puro sem dependência Scoped (`OutboxEventTypeResolver`, `TimeProvider`) | Singleton |
| `BackgroundService` | Singleton (hosted) → cria escopo por iteração |

**Captive dependency**: Singleton que recebe Scoped (direto ou via Transient) "captura" uma instância do primeiro escopo — DbContext compartilhado entre threads e, aqui, **tenant do primeiro request para todo mundo**. É o pior bug de DI possível num sistema multi-tenant.

Hoje a maioria é Transient sobre DbContext Scoped. Funciona (o Transient recebe o DbContext do escopo), mas é inconsistente (Dashboard/Email são Scoped) e cria instâncias à toa. Em código novo use Scoped; não mude registros existentes em massa sem motivo.

**Incorreto:**

```csharp
services.AddSingleton<INotificacaoService, NotificacaoService>();   // NotificacaoService → AppDbContext (Scoped)

public sealed class PushDispatcherWorker(INotificacaoEntregaRepositorio repo) : BackgroundService { } // captive
```

**Correto:**

```csharp
services.AddScoped<INotificacaoService, NotificacaoService>();

public sealed class PushDispatcherWorker(IServiceScopeFactory scopeFactory, ILogger<PushDispatcherWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificacaoEntregaRepositorio>();
        // ...
    }
}
```

`ValidateScopes = true` em Development detecta captive dependency no startup.

**No TruckFlow:** `CurrentUserService` Transient vs `CurrentUserGuard` Scoped (inconsistente, não perigoso). Workers usam `IServiceScopeFactory` — correto.

Referência: [Service lifetimes](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection#service-lifetimes) · [Andrew Lock — Captive dependencies](https://andrewlock.net/the-dangers-and-gotchas-of-using-scoped-services-when-configuring-options-in-asp-net-core/) · [Mark Seemann — Dependency Injection Principles, Practices, and Patterns](https://blog.ploeh.dk/)
