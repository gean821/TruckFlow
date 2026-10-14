---
title: Ajuste autovacuum nas tabelas de fila e alto churn
impact: LOW-MEDIUM
impactDescription: evita bloat, planos ruins e index-only scan degradado
tags: vacuum, autovacuum, analyze, bloat, queue-tables
---

## Ajuste autovacuum nas tabelas de fila e alto churn

Tabelas de fila (`"OutboxEvent"`, `"NotificacaoEntrega"`) sofrem UPDATE em quase toda linha (pendente → processado) e acumulam tuplas mortas. Com o default (`autovacuum_vacuum_scale_factor = 0.2`), uma tabela de 5M linhas só é aspirada após 1M tuplas mortas — o índice parcial de pendentes incha e o claim do worker fica lento.

**Incorreto (defaults em tabela de fila grande):**

```sql
SELECT relname, n_live_tup, n_dead_tup, last_autovacuum, last_autoanalyze
FROM pg_stat_user_tables ORDER BY n_dead_tup DESC LIMIT 10;
-- "OutboxEvent" | 4.8M | 1.9M | 3 dias atrás
```

**Correto (thresholds por tabela):**

```sql
ALTER TABLE "OutboxEvent" SET (
    autovacuum_vacuum_scale_factor = 0.01,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_cost_limit = 1000
);
ALTER TABLE "NotificacaoEntrega" SET (autovacuum_vacuum_scale_factor = 0.02);
```

```csharp
// em migration
migrationBuilder.Sql("""ALTER TABLE "OutboxEvent" SET (autovacuum_vacuum_scale_factor = 0.01, autovacuum_analyze_scale_factor = 0.02);""");
```

Também: política de retenção para filas (apagar `"OutboxEvent"` processado > 30 dias em lotes, ou particionar por mês); `ANALYZE` manual depois de backfill grande; transações longas abertas (`conn-idle-timeout`) impedem o VACUUM de limpar qualquer coisa — é a causa mais comum de bloat "misterioso".

**No TruckFlow:** filas em `OutboxEventConfiguracao.cs`/`NotificacaoEntregaConfiguracao.cs`; `AuditLog` é append-only (cresce, não incha) — candidato a particionamento/retention, não a tuning de vacuum.

Referência: [Routine Vacuuming](https://www.postgresql.org/docs/16/routine-vacuuming.html) · [Cybertec — autovacuum tuning](https://www.cybertec-postgresql.com/en/tuning-autovacuum-postgresql/) · [Crunchy Data — Bloat](https://www.crunchydata.com/blog/postgres-performance-boost-hot-updates-and-fill-factor)
