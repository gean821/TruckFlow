---
title: Use lock_timeout em migrations para não enfileirar o tráfego atrás de um ALTER
impact: CRITICAL
impactDescription: evita parada total causada por fila de locks atrás de DDL
tags: migrations, lock-timeout, ddl, access-exclusive
---

## Use lock_timeout em migrations para não enfileirar o tráfego atrás de um ALTER

Até `ALTER TABLE ... ADD COLUMN` instantâneo precisa de `ACCESS EXCLUSIVE` por um instante. Se uma transação longa (relatório, worker) segura a tabela, o `ALTER` **espera** — e todo SELECT/INSERT novo entra na fila **atrás** dele. A API para inteira enquanto o DDL nem começou.

**Incorreto (DDL sem limite de espera):**

```sql
ALTER TABLE "Agendamento" ADD COLUMN "Observacao" text;
-- esperando lock... enquanto isso 300 requests em fila
```

**Correto (falhar rápido e tentar de novo):**

```sql
SET lock_timeout = '3s';
SET statement_timeout = '60s';
ALTER TABLE "Agendamento" ADD COLUMN "Observacao" text;
```

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("SET lock_timeout = '3s';");
    migrationBuilder.AddColumn<string>(name: "Observacao", table: "Agendamento", type: "text", nullable: true);
}
```

`SET` (sem `LOCAL`) vale para a sessão da migration — ok, o job de migration é uma conexão própria. O pipeline deve ter retry com backoff se a migration falhar por `lock_timeout` (SQLSTATE `55P03`).

Operações que reescrevem a tabela (evitar em tabela quente): mudar tipo de coluna incompatível, `ADD COLUMN ... DEFAULT volatile()`, `VACUUM FULL`, `CLUSTER`. `ADD COLUMN ... DEFAULT constante` é instantâneo desde o PG 11.

**No TruckFlow:** junto de `migration-out-of-startup` — só faz sentido com migration em job separado com retry.

Referência: [Explicit Locking — table-level lock modes](https://www.postgresql.org/docs/16/explicit-locking.html) · [Xata/pgroll — lock queues](https://xata.io/blog/migrations-and-exclusive-locks) · [depesz — lock_timeout](https://www.depesz.com/2019/09/26/how-to-run-short-alter-table-without-long-locking-concurrent-queries/)
