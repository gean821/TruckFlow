---
title: Configure timeouts para transação ociosa e statements longos
impact: HIGH
impactDescription: libera locks e conexões presos por request abortado
tags: connections, timeout, idle-in-transaction, statement-timeout
---

## Configure timeouts para transação ociosa e statements longos

Uma transação aberta e esquecida (`idle in transaction`) segura locks e impede o VACUUM de limpar linhas mortas. Request cancelado no meio de `BeginTransactionAsync` sem `await using` é a fonte clássica.

**Incorreto (sem limites):**

```sql
-- conexões "idle in transaction" há horas segurando lock em "Agendamento"
SELECT pid, state, now() - xact_start AS duracao, query
FROM pg_stat_activity WHERE state = 'idle in transaction';
```

```csharp
var tx = await _db.Database.BeginTransactionAsync(ct); // sem await using: exceção deixa tx pendurada até o pool reciclar
```

**Correto (limites no role da aplicação + dispose garantido):**

```sql
ALTER ROLE truckflow_app SET idle_in_transaction_session_timeout = '30s';
ALTER ROLE truckflow_app SET statement_timeout = '30s';
ALTER ROLE truckflow_app SET lock_timeout = '5s';
```

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
// ...
await tx.CommitAsync(ct);
```

Defina no **role da aplicação**, não globalmente: o job de migration (`migration-lock-timeout`) e relatórios precisam de valores diferentes. `Command Timeout` do Npgsql é o equivalente do lado cliente.

**No TruckFlow:** o padrão `await using var tx = await _repo.BeginTransactionAsync(token)` em `AgendamentoMotoristaService.BookAppointment` está correto — siga-o.

Referência: [Client Connection Defaults](https://www.postgresql.org/docs/16/runtime-config-client.html) · [Laurenz Albe — idle in transaction](https://www.cybertec-postgresql.com/en/idle_in_transaction_session_timeout-terminating-idle-transactions-in-postgresql/)
