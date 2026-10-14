---
title: Workers em multi-instância reivindicam trabalho com FOR UPDATE SKIP LOCKED
impact: CRITICAL
impactDescription: 2+ instâncias processam a fila em paralelo sem duplicar push/expiração
tags: skip-locked, queue, workers, outbox, multi-instance
---

## Workers em multi-instância reivindicam trabalho com FOR UPDATE SKIP LOCKED

`BackgroundService` roda em **toda** instância da API. Sem claim atômico, duas instâncias pegam o mesmo evento do outbox e o motorista recebe 2 pushes, ou duas expiram o mesmo agendamento e uma estoura concorrência. `FOR UPDATE SKIP LOCKED` faz cada worker travar linhas diferentes sem esperar o outro.

**Incorreto (SELECT + processa + UPDATE):**

```csharp
var pendentes = await _db.OutboxEvent
    .Where(e => e.ProcessedAt == null)
    .OrderBy(e => e.CreatedAt).Take(50).ToListAsync(ct);   // as duas instâncias leem os mesmos 50
foreach (var e in pendentes) { await Processar(e); e.MarcarProcessado(); }
await _db.SaveChangesAsync(ct);
```

**Correto (claim dentro de transação curta):**

```sql
BEGIN;
SELECT *, xmin FROM "NotificacaoEntrega"
WHERE "Status" = 0 AND "Canal" = 2
  AND ("ProximaTentativaEm" IS NULL OR "ProximaTentativaEm" <= now())
ORDER BY "CreatedAt"
FOR UPDATE SKIP LOCKED
LIMIT 1;
-- processa, atualiza status/tentativas
COMMIT;
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var entrega = await _db.NotificacaoEntrega
    .FromSqlRaw("""
        SELECT *, xmin FROM "NotificacaoEntrega"
        WHERE "Status" = 0 AND "Canal" = 2
          AND ("ProximaTentativaEm" IS NULL OR "ProximaTentativaEm" <= NOW())
        ORDER BY "CreatedAt"
        FOR UPDATE SKIP LOCKED
        LIMIT 1
        """)
    .IgnoreQueryFilters()           // fila é cross-tenant; o handler entra em WithTenant(entrega.EmpresaId)
    .FirstOrDefaultAsync(ct);
// ... processa
await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

Cuidados: o lock vive **só dentro da transação** — chamada HTTP longa (Expo, SEFAZ) dentro dela segura a linha; para lotes, marque `"Status" = Processando` + `"ClaimedAt"` e commite antes da chamada externa, com timeout de reclaim. Backoff via `"ProximaTentativaEm"` + índice parcial da fila (`query-partial-indexes`).

**No TruckFlow:** `OutboxProcessorWorker`, `NotificacaoEntregaRepositorio.ClaimNextPushPendenteAsync`/`ClaimLotePushPendenteReceiptAsync`. Pendente (ADR-0004 item 4): `AgendamentoExpirationService` ainda depende de `xmin` para não duplicar — migrar o SELECT para `SKIP LOCKED`. Teste: `OutboxDispatcherConcorrenciaTests`.

Referência: [SELECT — The Locking Clause](https://www.postgresql.org/docs/16/sql-select.html#SQL-FOR-UPDATE-SHARE) · [Brandur — Transactionally staged job drains](https://brandur.org/job-drain) · [Crunchy Data — Message queuing with SKIP LOCKED](https://www.crunchydata.com/blog/message-queuing-using-native-postgresql)
