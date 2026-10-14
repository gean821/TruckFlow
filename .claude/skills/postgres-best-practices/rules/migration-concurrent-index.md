---
title: Crie índice em tabela quente com CONCURRENTLY (fora da transação da migration)
impact: CRITICAL
impactDescription: zero bloqueio de escrita em vez de minutos com a tabela travada
tags: migrations, create-index-concurrently, ef-core, zero-downtime
---

## Crie índice em tabela quente com CONCURRENTLY (fora da transação da migration)

`CREATE INDEX` comum pega lock `SHARE` — bloqueia INSERT/UPDATE/DELETE até terminar. Em `"Agendamento"`, `"Notificacao"` ou `"MotoristaPosicaoHistorico"` em horário de pico, isso para a portaria das 21 fábricas. `CONCURRENTLY` não bloqueia escrita, mas **não pode rodar dentro de transação** — e o EF embrulha cada migration numa.

**Incorreto (o que `HasIndex` gera por padrão):**

```csharp
// migration gerada a partir de builder.HasIndex(...)
migrationBuilder.CreateIndex(
    name: "IX_Notificacao_EmpresaId_Prioridade",
    table: "Notificacao",
    columns: new[] { "EmpresaId", "Prioridade" });
```

```sql
CREATE INDEX "IX_Notificacao_EmpresaId_Prioridade" ON "Notificacao" ("EmpresaId", "Prioridade");
-- tabela travada para escrita durante todo o build
```

**Correto (SQL explícito, sem transação):**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Notificacao_EmpresaId_Prioridade" ON "Notificacao" ("EmpresaId", "Prioridade");""",
        suppressTransaction: true);
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """DROP INDEX CONCURRENTLY IF EXISTS "IX_Notificacao_EmpresaId_Prioridade";""",
        suppressTransaction: true);
}
```

Mantenha o `HasIndex(...)` na configuração (o model snapshot precisa saber do índice) e **substitua** o `CreateIndex` gerado pelo `Sql(...)`. Se o `CONCURRENTLY` falhar no meio, fica um índice `INVALID` — o `IF NOT EXISTS` não o recria; dropar e rodar de novo:

```sql
SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;
```

Tabela pequena (cadastros: `"LocalDescarga"`, `"Fornecedor"`) pode usar o `CreateIndex` normal — o lock dura milissegundos.

**No TruckFlow:** migrations em `TruckFlowApi.Infra/Migrations/`; o padrão `migrationBuilder.Sql` já é usado em `20260510033426_agendamento-overlap-exclusion.cs`.

Referência: [CREATE INDEX CONCURRENTLY](https://www.postgresql.org/docs/16/sql-createindex.html#SQL-CREATEINDEX-CONCURRENTLY) · [EF Core — Custom migration operations / suppressTransaction](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing#arbitrary-changes-via-raw-sql) · [Braintree — Safe operations for high volume PostgreSQL](https://medium.com/paypal-tech/postgresql-at-scale-database-schema-changes-without-downtime-20d3749ed680)
