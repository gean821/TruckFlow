---
title: Diagnostique com ToQueryString/LogTo + EXPLAIN (ANALYZE, BUFFERS) — não adivinhe
impact: LOW-MEDIUM
impactDescription: acha a causa real (seq scan, sort em disco, estimativa errada) em minutos
tags: explain, explain-analyze, ef-core, toquerystring, diagnostics
---

## Diagnostique com ToQueryString/LogTo + EXPLAIN (ANALYZE, BUFFERS) — não adivinhe

O LINQ que você escreveu não é o SQL que roda: o filtro global adiciona `"EmpresaId"`, `Include` vira JOIN, `Contains` vira `= ANY`. Primeiro pegue o SQL real; depois o plano real.

**Incorreto:**

```csharp
// "deve ser falta de índice" → cria índice aleatório e sobe
builder.HasIndex(x => x.UpdatedAt);
```

**Correto (1: SQL gerado):**

```csharp
var q = _db.Agendamento.Where(a => a.StatusAgendamento == StatusAgendamento.Agendado).OrderBy(a => a.DataInicio);
Console.WriteLine(q.ToQueryString());   // SQL com parâmetros (inclui o filtro de tenant)

// dev: log de todo SQL com tempo
options.UseNpgsql(cs).LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
       .EnableSensitiveDataLogging();   // NUNCA em produção (dados pessoais/fiscais nos logs)
```

**Correto (2: plano real no psql com os mesmos parâmetros):**

```sql
EXPLAIN (ANALYZE, BUFFERS, VERBOSE, SETTINGS)
SELECT a.* FROM "Agendamento" a
WHERE a."EmpresaId" = '...' AND a."StatusAgendamento" = 2
ORDER BY a."DataInicio";
```

O que procurar:

| No plano | Significa | Ação |
|---|---|---|
| `Seq Scan` com `Rows Removed by Filter` alto | sem índice útil | `query-composite-indexes` |
| `rows=10` estimado vs `rows=50000` real | estatística velha / correlação | `ANALYZE`; `CREATE STATISTICS` |
| `Sort Method: external merge Disk` | `work_mem` baixo ou sort evitável | índice na ordem do `ORDER BY` |
| `Heap Fetches` alto em Index Only Scan | visibility map desatualizado | VACUUM (`monitor-vacuum-analyze`) |
| `Nested Loop` com loops=10000 | N+1 dentro do banco | reescrever JOIN / índice no lado interno |

`EXPLAIN ANALYZE` **executa** a query — em `UPDATE/DELETE`, embrulhe em `BEGIN; ... ROLLBACK;`. Cole planos complexos em explain.depesz.com ou explain.dalibo.com.

**No TruckFlow:** `TruckFlow.Benchmarks` (BenchmarkDotNet) e `TruckFlow.LoadTests` (NBomber) existem para medir antes/depois.

Referência: [Using EXPLAIN](https://www.postgresql.org/docs/16/using-explain.html) · [EF Core — Simple logging / ToQueryString](https://learn.microsoft.com/ef/core/logging-events-diagnostics/simple-logging) · [depesz — explain.depesz.com](https://explain.depesz.com/) · [pganalyze — EXPLAIN guide](https://pganalyze.com/docs/explain)
