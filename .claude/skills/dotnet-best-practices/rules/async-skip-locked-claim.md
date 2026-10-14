---
title: Fila de trabalho entre instâncias = claim com FOR UPDATE SKIP LOCKED
impact: HIGH
impactDescription: 2 instâncias nunca processam o mesmo evento/push/expiração
tags: concurrency, multi-instance, postgres, skip-locked, worker, outbox
---

## Fila de trabalho entre instâncias = claim com FOR UPDATE SKIP LOCKED

Com 2+ instâncias da API (pré-requisito de rollout, ADR-0004), todo worker roda N vezes em paralelo. `SELECT ... WHERE status = pendente` sem lock faz duas instâncias pegarem o mesmo item (push duplicado, evento processado duas vezes). Lock otimista (`xmin`) só detecta depois. O padrão é **claim** atômico dentro de uma transação curta: `FOR UPDATE SKIP LOCKED` faz cada instância pular as linhas já travadas pela outra.

**Incorreto:**

```csharp
var pendentes = await db.OutboxEvent
    .Where(e => e.ProcessadoEm == null && e.ProximaTentativaEm <= agora)
    .Take(50).ToListAsync(ct);         // as duas instâncias leem os mesmos 50
```

**Correto:**

```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);
var evento = await db.OutboxEvent
    .FromSql($"""
        SELECT * FROM "OutboxEvent"
        WHERE "ProcessadoEm" IS NULL AND "ProximaTentativaEm" <= {agora}
        ORDER BY "CriadoEm"
        LIMIT 1
        FOR UPDATE SKIP LOCKED
        """)
    .FirstOrDefaultAsync(ct);
if (evento is null) return;
// processa sob WithTenant(evento.EmpresaId), marca processado, commit
await tx.CommitAsync(ct);
```

Regras: transação curta (sem I/O externo longo segurando o lock — para push, marque "em envio" e comite, envie, depois registre o resultado); `ORDER BY` com índice parcial nas pendentes; `FromSql` interpolado (parametrizado, sem SQL injection). Para "só uma instância roda este job" (não fila), `pg_try_advisory_lock` é alternativa.

**No TruckFlow:** `OutboxProcessorWorker` e `NotificacaoEntregaRepositorio` usam; `AgendamentoExpirationService` ainda não (ADR-0004 item 4). Provado em `OutboxDispatcherConcorrenciaTests`.

Referência: [PostgreSQL — SELECT ... FOR UPDATE SKIP LOCKED](https://www.postgresql.org/docs/current/sql-select.html#SQL-FOR-UPDATE-SHARE) · [2ndQuadrant/EDB — What is SKIP LOCKED for in PostgreSQL 9.5?](https://www.enterprisedb.com/blog/what-skip-locked-postgresql-95) · [EF Core — SQL queries (FromSql)](https://learn.microsoft.com/ef/core/querying/sql-queries)
