---
title: ExecuteUpdate/ExecuteDelete e SQL cru não passam pelos interceptors
impact: CRITICAL
impactDescription: evita update em massa cross-tenant e buracos na trilha de auditoria e no outbox
tags: multi-tenant, ef-core, bulk, audit, raw-sql, outbox
---

## ExecuteUpdate/ExecuteDelete e SQL cru não passam pelos interceptors

`ExecuteUpdateAsync`/`ExecuteDeleteAsync` **respeitam** o global query filter (a query parte do `DbSet`), mas **não** passam pelo `SaveChanges` — logo `EmpresaScopedSaveChangesInterceptor`, `AuditSaveChangesInterceptor` e `OutboxSaveChangesInterceptor` não rodam. Com `IgnoreQueryFilters()` ou `ExecuteSql`/`FromSql`, nem o filtro existe. Use bulk só quando o ganho é real, filtre `EmpresaId` explicitamente e decida conscientemente o que acontece com auditoria e eventos.

**Incorreto:**

```csharp
await _db.Database.ExecuteSqlAsync(
    $"UPDATE \"Agendamento\" SET \"StatusAgendamento\" = 6 WHERE \"DataFim\" < {agora}", ct);
// todas as empresas, sem AuditLog, sem evento de domínio
```

**Correto:**

```csharp
var empresaId = _empresaContext.EmpresaId;
await _db.Agendamento
    .Where(a => a.EmpresaId == empresaId          // explícito, mesmo com filtro global
             && a.StatusAgendamento == StatusAgendamento.Disponivel
             && a.DataFim < agora)
    .ExecuteUpdateAsync(s => s
        .SetProperty(a => a.StatusAgendamento, StatusAgendamento.Expirado)
        .SetProperty(a => a.UpdatedAt, agora), ct);
```

Se a transição precisa de evento de domínio (notificar motorista), **não** use bulk: carregue em lotes (ex.: 500) e chame o método da entidade (`agendamento.Expirar()`), como `AgendamentoExpirationService` faz.

**No TruckFlow:** hot paths que vão usar SQL cru (upsert de `MotoristaPosicaoAtual`, ADR-0003) devem receber `EmpresaId` do contexto como parâmetro. `GradeRepositorio` usa `ExecuteDeleteAsync` — confira se o delete não deveria gerar auditoria.

Referência: [EF Core — ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete) · [EF Core Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)
