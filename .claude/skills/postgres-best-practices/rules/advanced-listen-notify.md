---
title: LISTEN/NOTIFY para fan-out do SSE — payload pequeno, conexão dedicada, fora do PgBouncer
impact: MEDIUM
impactDescription: realtime entre N instâncias sem Redis; sem perder evento por payload grande
tags: listen-notify, pg-notify, sse, realtime, multi-instance
---

## LISTEN/NOTIFY para fan-out do SSE — payload pequeno, conexão dedicada, fora do PgBouncer

ADR-0002: o admin conectado na instância B precisa receber a notificação gravada pela instância A. `pg_notify` no commit + `LISTEN` em cada instância resolve sem broker externo. Mas NOTIFY tem limites que viram bug silencioso.

**Incorreto:**

```sql
-- payload com a notificação inteira (Corpo markdown, Payload jsonb...) → passa de 8000 bytes e falha
SELECT pg_notify('notif_realtime', row_to_json(n)::text) FROM "Notificacao" n WHERE ...;
```

```csharp
// LISTEN numa conexão do pool / atrás de PgBouncer transaction mode → para de receber sem erro
await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
```

**Correto:**

```sql
-- payload mínimo: ids + tenant; o consumidor busca o resto se precisar
SELECT pg_notify('notif_realtime', json_build_object(
    'id', $1, 'empresaId', $2, 'destinatarioUsuarioId', $3)::text);
```

```csharp
// listener dedicado (BackgroundService), conexão própria e direta ao Postgres
await using var conn = new NpgsqlConnection(_listenConnectionString); // sem PgBouncer
await conn.OpenAsync(ct);
conn.Notification += (_, e) => _manager.Publicar(e.Payload);
await using (var cmd = new NpgsqlCommand("LISTEN notif_realtime", conn)) await cmd.ExecuteNonQueryAsync(ct);
while (!ct.IsCancellationRequested) await conn.WaitAsync(ct);    // reconectar com backoff ao cair
```

Propriedades a lembrar: NOTIFY dentro de transação só é entregue **no COMMIT** (rollback descarta — bom); payload máximo **8000 bytes**; entrega é *at-most-once* para quem está ouvindo — quem estava desconectado perde, por isso o front **invalida a query** ao receber e refaz o GET (fonte da verdade é a tabela, NOTIFY é só o "sininho"); fila de notificações tem limite (8GB) — listener lento trava `NOTIFY` dos produtores. Filtrar por `EmpresaId` antes de enviar ao cliente SSE.

**No TruckFlow:** `RealtimeNotificationInterceptor` (canal `notif_realtime`, `SELECT pg_notify(...)`), `RealtimeNotificationListener` (conexão dedicada, reconecta em 5s), `SseNotificationStreamer` (heartbeat 25s). Front: `useRealtimeNotifications.ts` invalida as queries de notificação.

Referência: [NOTIFY](https://www.postgresql.org/docs/16/sql-notify.html) · [LISTEN](https://www.postgresql.org/docs/16/sql-listen.html) · [Npgsql — Waiting for notifications](https://www.npgsql.org/doc/wait.html) · [Brandur — Notifier pattern](https://brandur.org/notifier)
