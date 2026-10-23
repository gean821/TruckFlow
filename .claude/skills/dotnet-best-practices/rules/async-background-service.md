---
title: BackgroundService com PeriodicTimer, escopo por iteração e falha isolada
impact: MEDIUM-HIGH
impactDescription: worker não morre na primeira exceção, não segura DbContext eterno e para limpo no deploy
tags: background-service, hosted-service, worker, cancellation
---

## BackgroundService com PeriodicTimer, escopo por iteração e falha isolada

Anatomia de worker no TruckFlow:
1. Singleton hosted; dependências Scoped via `IServiceScopeFactory.CreateAsyncScope()` **por iteração** (DbContext novo a cada ciclo, change tracker não cresce para sempre).
2. Loop com `PeriodicTimer` (sem drift e sem `Task.Delay` acumulando) respeitando `stoppingToken`.
3. `try/catch` **dentro** do loop: exceção não tratada em `ExecuteAsync` para o host (.NET 8: `BackgroundServiceExceptionBehavior.StopHost` por padrão) — derruba a API inteira.
4. `OperationCanceledException` com `stoppingToken` cancelado = shutdown normal, sai sem logar erro.
5. Trabalho por tenant com `WithTenant` (ver `tenant-workers-with-tenant`) e por lote (ex.: 500).
6. Seguro para **2+ instâncias** (ADR-0004 item 1/4): claim com `FOR UPDATE SKIP LOCKED` ou idempotência.

**Incorreto:**

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    var db = _sp.GetRequiredService<AppDbContext>();        // Scoped resolvido do root → captive
    while (true)
    {
        await Processar(db);                                 // 1 exceção mata o worker (e o host)
        await Task.Delay(60000);                             // ignora cancelamento
    }
}
```

**Correto:**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
    do
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IAgendamentoExpirationService>();
            var empresaContext = scope.ServiceProvider.GetRequiredService<IEmpresaContext>();
            // foreach empresa → WithTenant → service.ExpirarVencidosAsync(stoppingToken)
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ciclo do {Worker} falhou; tentando no próximo.", nameof(AgendamentoExpirationWorker));
        }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
}
```

**No TruckFlow:** `AgendamentoExpirationWorker`, `OutboxProcessorWorker`, `PushDispatcherWorker`, `ReceiptPollerWorker`, `RealtimeNotificationListener` (conexão LISTEN dedicada com reconexão em 5s). ADR-0004 item 4: expiração precisa de claim distribuído antes do multi-instance.

Referência: [Background tasks with hosted services](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services) · [.NET 6+ — unhandled exceptions in BackgroundService](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling) · [PeriodicTimer](https://learn.microsoft.com/dotnet/api/system.threading.periodictimer)
