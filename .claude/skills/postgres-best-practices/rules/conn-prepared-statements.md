---
title: Entenda prepared statements do Npgsql antes de ligar PgBouncer
impact: MEDIUM
impactDescription: evita erros "prepared statement does not exist" com pooler
tags: prepared-statements, npgsql, pgbouncer, auto-prepare
---

## Entenda prepared statements do Npgsql antes de ligar PgBouncer

Npgsql suporta prepared statements explícitos e **auto-prepare** (`Max Auto Prepare=N`), que dá 10-30% em queries repetidas. Mas prepared statements são estado de **sessão**: em PgBouncer modo transaction, a próxima transação pode cair em outro backend.

**Incorreto (auto-prepare + PgBouncer transaction antigo):**

```text
Host=pgbouncer;Max Auto Prepare=20;...   # PgBouncer < 1.21 → "prepared statement _auto0 does not exist"
```

**Correto (escolha consciente):**

```text
# Direto no Postgres (sem pooler): pode ganhar com auto-prepare
Host=db;Max Auto Prepare=20;Auto Prepare Min Usages=5;...

# Atrás de PgBouncer transaction mode:
Host=pgbouncer;Max Auto Prepare=0;No Reset On Close=true;...
# ou PgBouncer >= 1.21 com max_prepared_statements=200 no pgbouncer.ini
```

O EF Core já parametriza todas as queries — o plano é cacheado do lado do servidor só se a query for preparada. Sem auto-prepare, você ainda ganha cache de query compilada do EF (lado cliente).

**No TruckFlow:** hoje sem auto-prepare e sem PgBouncer. Ao implementar ADR-0004 item 8, decida isso **junto** e teste o `RealtimeNotificationListener` (LISTEN) fora do pooler.

Referência: [Npgsql — Prepared Statements](https://www.npgsql.org/doc/prepare.html) · [PgBouncer 1.21 prepared statements](https://www.pgbouncer.org/config.html#max_prepared_statements)
