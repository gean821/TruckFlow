---
title: Previna deadlock travando linhas em ordem consistente
impact: MEDIUM-HIGH
impactDescription: elimina erros 40P01 intermitentes sob carga
tags: deadlock, locks, ordering, transactions
---

## Previna deadlock travando linhas em ordem consistente

Deadlock acontece quando T1 trava A e espera B enquanto T2 trava B e espera A. O Postgres mata uma delas (`40P01 deadlock_detected`) depois de `deadlock_timeout`. Aparece em operações que tocam várias linhas: reagendar (vaga antiga + nova), atualização em lote de grade, conferência com N itens.

**Incorreto (ordem depende da entrada):**

```csharp
// Reagendar: trava a origem, depois o destino
var origem = await Travar(dto.OrigemId);
var destino = await Travar(dto.DestinoId);   // outro request reagendando destino→origem trava ao contrário
```

```sql
-- T1: UPDATE ... WHERE "Id" = 'aaa'; UPDATE ... WHERE "Id" = 'bbb';
-- T2: UPDATE ... WHERE "Id" = 'bbb'; UPDATE ... WHERE "Id" = 'aaa';  → deadlock
```

**Correto (ordem determinística por Id):**

```sql
SELECT * FROM "Agendamento" WHERE "Id" = ANY($1) ORDER BY "Id" FOR UPDATE;
```

```csharp
var ids = new[] { dto.OrigemId, dto.DestinoId }.Order().ToArray();
var vagas = await _db.Agendamento
    .FromSqlInterpolated($"""SELECT *, xmin FROM "Agendamento" WHERE "Id" = ANY({ids}) ORDER BY "Id" FOR UPDATE""")
    .ToListAsync(ct);
```

Também: atualizações em lote ordenadas por PK; transações curtas (`lock-short-transactions`); se ainda ocorrer, trate `40P01` como erro transitório com **um** retry (diferente de conflito de negócio).

```sql
SELECT deadlocks FROM pg_stat_database WHERE datname = current_database();
```

**No TruckFlow:** `AgendamentoAdminService` (reagendar/cancelar/finalizar) e `GradeView` no front atualizando grades em loop (N PATCHs) são os candidatos. Habilite `log_lock_waits = on` no servidor.

Referência: [Deadlocks](https://www.postgresql.org/docs/16/explicit-locking.html#LOCKING-DEADLOCKS) · [Cybertec — How to fix deadlocks](https://www.cybertec-postgresql.com/en/postgresql-understanding-deadlocks/)
