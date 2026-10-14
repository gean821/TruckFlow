---
title: RLS como defesa em profundidade (opcional) — com o caveat de pooling
impact: MEDIUM-HIGH
impactDescription: barreira no banco contra SQL cru, ExecuteUpdate e bugs de filtro
tags: rls, row-level-security, multi-tenant, defense-in-depth, pgbouncer
---

## RLS como defesa em profundidade (opcional) — com o caveat de pooling

O filtro global do EF **não protege**: SQL cru (`FromSqlRaw` sem `WHERE`), `ExecuteUpdate/Delete` que esqueceram o tenant, psql de suporte, outra aplicação no mesmo banco. Row-Level Security no Postgres fecha essas portas — mas exige setar o tenant **por transação** na conexão.

**Incorreto (RLS com SET de sessão + pool):**

```sql
SET app.empresa_id = '...';  -- vale para a SESSÃO: a conexão volta ao pool com o tenant do request anterior
```

**Correto (policy + SET LOCAL por transação):**

```sql
ALTER TABLE "Agendamento" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Agendamento" FORCE ROW LEVEL SECURITY; -- vale até para o dono da tabela

CREATE POLICY tenant_isolation ON "Agendamento"
    USING ("EmpresaId" = current_setting('app.empresa_id', true)::uuid)
    WITH CHECK ("EmpresaId" = current_setting('app.empresa_id', true)::uuid);
-- current_setting(..., true) devolve NULL sem setting → comparação NULL → zero linhas (fail-closed)
```

```csharp
// DbConnectionInterceptor / DbTransactionInterceptor: no início de cada transação
await using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT set_config('app.empresa_id', @e, true)"; // true = LOCAL à transação
cmd.Parameters.Add(new NpgsqlParameter("e", empresaId.ToString()));
await cmd.ExecuteNonQueryAsync(ct);
```

Custos e caveats:
- `SET LOCAL`/`set_config(..., true)` só vale **dentro de transação** — queries fora de transação explícita precisariam ser embrulhadas.
- Workers cross-tenant e claims de fila precisam de role com `BYPASSRLS` ou policy específica.
- Performance: a policy vira predicado em toda query — mantenha `EmpresaId` como coluna líder dos índices.

**No TruckFlow:** não implementado hoje (isolamento é via EF). Candidato para resposta forte no SAD Aurora (AppSec/DataSec) e pré-pentest (ADR-0004 item 16). Avalie com `risk-manager` antes.

Referência: [Row Security Policies](https://www.postgresql.org/docs/16/ddl-rowsecurity.html) · [Crunchy Data — Row Level Security for tenants](https://www.crunchydata.com/blog/row-level-security-for-tenants-in-postgres) · [Laurenz Albe — RLS and connection pools](https://www.cybertec-postgresql.com/en/row-level-security-with-connection-pooling/)
