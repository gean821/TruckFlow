---
title: Use pg_stat_statements para achar as queries que realmente custam
impact: LOW-MEDIUM
impactDescription: prioriza otimização pelo tempo total, não pela query mais "feia"
tags: pg-stat-statements, monitoring, observability, performance
---

## Use pg_stat_statements para achar as queries que realmente custam

A query que mais pesa raramente é a mais lenta — é a de 5ms chamada 2 milhões de vezes (polling de `unread-count`, `refetchInterval` de agendamentos). `pg_stat_statements` agrega por formato de query.

**Incorreto:**

```text
"Achei uma query de 800ms no log, vou otimizar essa" — roda 3x por dia.
```

**Correto:**

```sql
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;  -- requer shared_preload_libraries (já vem em managed)

SELECT calls,
       round(total_exec_time::numeric, 0)           AS total_ms,
       round(mean_exec_time::numeric, 2)            AS media_ms,
       round(100 * total_exec_time / sum(total_exec_time) OVER (), 1) AS pct,
       rows,
       left(query, 120)                             AS query
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;

-- depois de otimizar e fazer deploy, zere para medir de novo
SELECT pg_stat_statements_reset();
```

Correlacione com o código: use `TagWith` no LINQ para o comentário aparecer no SQL e nos logs do servidor.

```csharp
var q = _db.Notificacao.TagWith("NotificacaoService.ContarNaoLidas")
    .CountAsync(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null, ct);
```

**No TruckFlow:** ADR-0004 item 9 (OpenTelemetry → Prometheus/Grafana) — exporte métricas do `pg_stat_statements` (postgres_exporter) e a instrumentação `OpenTelemetry.Instrumentation.EntityFrameworkCore`/Npgsql para ter latência por query por tenant.

Referência: [pg_stat_statements](https://www.postgresql.org/docs/16/pgstatstatements.html) · [EF Core — Query tags](https://learn.microsoft.com/ef/core/querying/tags) · [Npgsql — OpenTelemetry tracing](https://www.npgsql.org/doc/diagnostics/tracing.html)
