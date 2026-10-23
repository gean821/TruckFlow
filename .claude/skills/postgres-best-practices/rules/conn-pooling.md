---
title: Dimensione o pool do Npgsql e planeje o PgBouncer com os caveats do modo transaction
impact: CRITICAL
impactDescription: evita "too many connections" com 2+ instâncias; 10-50x mais clientes com PgBouncer
tags: connection-pooling, npgsql, pgbouncer, multi-instance
---

## Dimensione o pool do Npgsql e planeje o PgBouncer com os caveats do modo transaction

O Npgsql já tem pool **por processo** (default `Maximum Pool Size=100`). Com N instâncias da API + workers + LISTEN dedicado, o total pode passar do `max_connections` do Postgres managed (muitas vezes 100). Cada conexão Postgres custa ~5-10MB.

**Incorreto (defaults multiplicados por instância):**

```text
# 3 instâncias × 100 (default) = 300 conexões possíveis → servidor com max_connections=100 cai sob pico
Host=db;Database=truckflow;Username=truckflow;Password=...
```

**Correto (pool explícito, somando < max_connections com folga):**

```text
Host=db;Database=truckflow;Username=truckflow_app;Password=...;
Maximum Pool Size=30;Minimum Pool Size=2;Connection Idle Lifetime=60;
Timeout=15;Command Timeout=30;Keepalive=30;Application Name=truckflow-api
```

```csharp
// Program.cs — AddDbContext já usa o pool do Npgsql; considere DbContext pooling para reduzir alocação:
builder.Services.AddDbContextPool<AppDbContext>(...); // ATENÇÃO: só se AppDbContext não guardar estado por request
```

`AddDbContextPool` **não** é compatível com o `AppDbContext` atual sem ajuste: ele recebe `IEmpresaContext` (scoped) no construtor e interceptors scoped — pool de contexto reaproveitaria o tenant do request anterior. Mantenha `AddDbContext` (é o seguro).

**PgBouncer (ADR-0004 item 8) em modo `transaction`:**
- Session state não sobrevive entre transações: `SET`, advisory lock de sessão, `LISTEN` → **quebram**.
- Prepared statements: Npgsql faz auto-prepare só se `Max Auto Prepare > 0` (default 0 — ok). PgBouncer ≥ 1.21 suporta `max_prepared_statements` se precisar.
- Use `No Reset On Close=true` quando atrás do PgBouncer.
- O `RealtimeNotificationListener` (LISTEN) precisa de **conexão direta** ao Postgres (connection string separada, sem PgBouncer).

**No TruckFlow:** `Program.cs:130` `UseNpgsql(GetConnectionString("DefaultConnection"))` sem parâmetros de pool; listener SSE em `RealtimeNotificationListener` com `NpgsqlConnection` dedicada.

Referência: [Npgsql — Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters.html) · [Npgsql — Compatibility with PgBouncer](https://www.npgsql.org/doc/compatibility.html#pgbouncer) · [PgBouncer features](https://www.pgbouncer.org/features.html) · [EF Core — DbContext pooling](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#dbcontext-pooling)
