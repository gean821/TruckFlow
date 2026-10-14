---
title: Calcule o orçamento de conexões por componente
impact: CRITICAL
impactDescription: previne exaustão de conexões e OOM do servidor
tags: connections, max-connections, limits, capacity
---

## Calcule o orçamento de conexões por componente

`max_connections` é recurso finito e caro. Faça a conta explícita antes de escalar instâncias ou adicionar worker.

**Incorreto (sem orçamento):**

```sql
-- "Aumenta pra 1000 que resolve"
ALTER SYSTEM SET max_connections = 1000; -- cada backend reserva memória; work_mem × conexões explode
```

**Correto (orçamento somado):**

```text
Componente                         Conexões
API (3 instâncias × pool 30)          90  (pico real bem menor — medir)
Workers (outbox/push/receipt/expiração) usam o mesmo pool por instância
LISTEN SSE (1 por instância)            3
Job de migration                        1
Admin/psql/monitoramento                5
Reserva superusuário (default)          3
-----------------------------------------
max_connections ≥ 102 → com PgBouncer, servidor fica em ~40 e o pooler absorve o resto
```

```sql
-- Uso atual por aplicação/estado
SELECT application_name, state, count(*)
FROM pg_stat_activity
GROUP BY 1, 2 ORDER BY 3 DESC;
```

Use `Application Name=truckflow-api` / `truckflow-worker` na connection string para essa query ser útil.

**No TruckFlow:** ADR-0001 estima 40-80 admins + 300-400 motoristas simultâneos no pico — requests HTTP curtas, não conexões persistentes. O consumo real é dominado pelo pool × instâncias. ADR-0004 item 8 (PgBouncer) é pré-rollout.

Referência: [Connections and Authentication](https://www.postgresql.org/docs/16/runtime-config-connection.html) · [Cybertec — max_connections tuning](https://www.cybertec-postgresql.com/en/tuning-max_connections-in-postgresql/)
