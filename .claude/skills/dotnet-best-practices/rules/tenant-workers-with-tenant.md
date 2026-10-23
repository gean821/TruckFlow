---
title: Workers iteram empresas e abrem WithTenant por empresa
impact: CRITICAL
impactDescription: background job não tem claim — sem WithTenant ele lança ou, pior, alguém "resolve" vendo tudo
tags: multi-tenant, background-service, worker
---

## Workers iteram empresas e abrem WithTenant por empresa

Fora de um request HTTP não existe claim `EmpresaId`. `EmpresaContext.EmpresaId` lança `UnauthorizedAccessException("Tenant não identificado.")` — comportamento certo (fail closed). O worker lista as empresas, abre `WithTenant(empresa.Id)` por iteração e isola a falha de uma empresa das demais.

**Incorreto:**

```csharp
// Sem tenant: alguém "resolve" com IgnoreQueryFilters e processa tudo misturado
var vencidos = await db.Agendamento.IgnoreQueryFilters()
    .Where(a => a.DataFim < agora).ToListAsync(ct);
```

**Correto:**

```csharp
foreach (var empresa in empresas)
{
    using var tenantScope = empresaContext.WithTenant(empresa.Id);
    try { await service.ExpirarVencidosAsync(stoppingToken); }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Falha ao expirar agendamentos da empresa {EmpresaId}. Próximas empresas seguem.",
            empresa.Id);
    }
}
```

Eventos de outbox carregam `EmpresaId` e o handler roda sob `WithTenant(outboxEvent.EmpresaId)`.

**No TruckFlow:** `TruckFlow/Extensions/Agendamento/AgendamentoExpirationWorker.cs` e `OutboxProcessorWorker` são a referência. ADR-0001/0004 exigem que contexto sem tenant resulte em **zero linhas** ou exceção, nunca em "sem filtro".

Referência: [Background tasks with hosted services](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services)
