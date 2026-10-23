---
title: ExecuteUpdate/ExecuteDelete são rápidos — e pulam interceptors (tenant, audit, outbox) e o xmin
impact: CRITICAL
impactDescription: bulk em 1 statement; sem cuidado, escrita sem auditoria e sem guarda de tenant
tags: executeupdate, executedelete, bulk, interceptors, audit, multi-tenant
---

## ExecuteUpdate/ExecuteDelete são rápidos — e pulam interceptors (tenant, audit, outbox) e o xmin

`ExecuteUpdateAsync`/`ExecuteDeleteAsync` viram um único `UPDATE`/`DELETE` no banco, sem carregar entidades. Ótimo para lote. Mas **não passam pelo `SaveChanges`**: `EmpresaScopedSaveChangesInterceptor`, `AuditSaveChangesInterceptor` e `OutboxSaveChangesInterceptor` **não rodam**, domain events não são publicados, `UpdatedAt`/`UpdatedBy` não são preenchidos e o token `xmin` é ignorado. O filtro global de **leitura** ainda se aplica ao `WHERE` (então o tenant é respeitado no LINQ) — mas SQL cru não.

**Incorreto (bulk onde precisava de trilha):**

```csharp
// Cancelar todos os agendamentos de uma grade
await _db.Agendamento
    .Where(a => a.GradeId == gradeId)
    .ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusAgendamento, StatusAgendamento.Cancelado), ct);
// sem AuditLog, sem AgendamentoCanceladoEvent → motoristas não são notificados; transição de status não validada
```

**Correto (escolha consciente):**

```csharp
// 1) Regra de negócio/evento/auditoria importam → carregar e usar o método de domínio
var vagas = await _db.Agendamento.Where(a => a.GradeId == gradeId).ToListAsync(ct);
foreach (var v in vagas) v.Cancelar("Grade removida");     // valida transição + domain event
await _db.SaveChangesAsync(ct);                            // interceptors rodam

// 2) Manutenção técnica sem semântica de negócio → bulk, com tenant e UpdatedAt explícitos
await _db.NotificacaoEntrega
    .Where(e => e.Status == StatusEntrega.Pendente && e.CreatedAt < corte)
    .ExecuteUpdateAsync(s => s
        .SetProperty(e => e.Status, StatusEntrega.Cancelado)
        .SetProperty(e => e.UpdatedAt, DateTime.UtcNow), ct);
```

```sql
UPDATE "NotificacaoEntrega" SET "Status" = 3, "UpdatedAt" = now()
WHERE "EmpresaId" = $1 AND "Status" = 0 AND "CreatedAt" < $2;
```

Checklist antes de usar bulk: o dado tem trilha de auditoria exigida (fiscal/LGPD)? Alguém precisa ser notificado (evento)? Há transição de status validada no domínio? Se sim para qualquer um → caminho 1. Lotes grandes: em fatias (`LIMIT` por Id) para não segurar lock em milhões de linhas.

**No TruckFlow:** hard deletes usam `ExecuteDeleteAsync` em alguns repositórios — confira se o registro precisava de `AuditLog`. `AgendamentoExpirationService` usa o caminho 1 (`agendamento.Expirar()` em lotes de 500) — correto.

Referência: [EF Core — ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete) · [EF Core — Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)
