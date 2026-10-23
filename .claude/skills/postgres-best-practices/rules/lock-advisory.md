---
title: Advisory lock (por transação) para jobs singleton entre instâncias
impact: MEDIUM
impactDescription: um único executor de job periódico sem tabela de lock
tags: advisory-locks, distributed-lock, workers, jobs
---

## Advisory lock (por transação) para jobs singleton entre instâncias

Alguns jobs devem rodar em **uma** instância por vez e não têm "linhas de fila" para `SKIP LOCKED`: criar/dropar partição mensal, recalcular dashboard, seed. Advisory lock é um mutex do Postgres identificado por um número.

**Incorreto (flag em memória ou tabela sem lock):**

```csharp
private static bool _rodando;            // vale por processo — 3 instâncias rodam 3 vezes
if (_rodando) return; _rodando = true;
```

**Correto (try-lock por transação):**

```sql
BEGIN;
SELECT pg_try_advisory_xact_lock(hashtext('truckflow:particoes-posicao'));  -- true só para um
-- se true: executa o job; o lock solta sozinho no COMMIT/ROLLBACK
COMMIT;
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var conseguiu = await _db.Database
    .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock(hashtext('truckflow:particoes-posicao')) AS \"Value\"")
    .SingleAsync(ct);
if (!conseguiu) return;              // outra instância está fazendo
await ExecutarManutencao(ct);
await tx.CommitAsync(ct);
```

Prefira a variante `_xact_` (por transação): advisory lock de **sessão** (`pg_advisory_lock`) não funciona atrás de PgBouncer em modo transaction e "vaza" se a conexão voltar ao pool sem unlock. Para filas com linhas, `SKIP LOCKED` é melhor que advisory.

**No TruckFlow:** candidato para o job de partições do rastreamento e para qualquer `BackgroundService` que não processe linhas individuais.

Referência: [Advisory Locks](https://www.postgresql.org/docs/16/explicit-locking.html#ADVISORY-LOCKS) · [Advisory Lock Functions](https://www.postgresql.org/docs/16/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)
