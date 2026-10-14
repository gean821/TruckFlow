---
title: Indexe jsonb só pelo que é consultado — GIN jsonb_path_ops ou índice de expressão
impact: LOW
impactDescription: 10-100x em filtros por chave de jsonb; evita GIN gigante desnecessário
tags: jsonb, gin, expression-index, payload
---

## Indexe jsonb só pelo que é consultado — GIN jsonb_path_ops ou índice de expressão

`jsonb` sem índice em `WHERE` faz seq scan com parse por linha. Mas GIN genérico em todo `jsonb` custa escrita e espaço. Indexe o padrão de consulta real.

**Incorreto:**

```sql
-- "Notificacao"."Payload" (jsonb) consultado por agendamentoId sem índice
SELECT * FROM "Notificacao" WHERE "EmpresaId" = $1 AND "Payload" ->> 'agendamentoId' = $2;
-- ou GIN default em tudo "por via das dúvidas"
CREATE INDEX ON "Notificacao" USING gin ("Payload");
```

**Correto:**

```sql
-- uma chave específica, igualdade → índice de expressão B-tree (pequeno, rápido)
CREATE INDEX "IX_Notificacao_Payload_AgendamentoId"
    ON "Notificacao" ("EmpresaId", (("Payload" ->> 'agendamentoId')));

-- containment em várias chaves → GIN jsonb_path_ops (menor que o default)
CREATE INDEX "IX_Empresa_Configuracoes" ON "Empresa" USING gin ("Configuracoes" jsonb_path_ops);
SELECT "Id" FROM "Empresa" WHERE "Configuracoes" @> '{"AuthMethods":["EntraId"]}';
```

```csharp
// Expression index em EF: via migration
migrationBuilder.Sql("""CREATE INDEX CONCURRENTLY "IX_Notificacao_Payload_AgendamentoId" ON "Notificacao" ("EmpresaId", (("Payload" ->> 'agendamentoId')));""", suppressTransaction: true);
// GIN pelo Fluent API (Npgsql)
builder.HasIndex(x => x.Configuracoes).HasMethod("gin").HasOperators("jsonb_path_ops");
```

Se uma chave do jsonb é filtrada o tempo todo, ela provavelmente devia ser **coluna** (ex.: `AgendamentoId` na `Notificacao`). O endpoint `/notifications/agendamento/{id}` é sinal disso.

**No TruckFlow:** `Notificacao.Payload` (jsonb com IDs de domínio, ADR-0002), `Empresa.Configuracoes`.

Referência: [JSON Types — jsonb Indexing](https://www.postgresql.org/docs/16/datatype-json.html#JSON-INDEXING) · [Npgsql — JSON querying](https://www.npgsql.org/efcore/mapping/json.html)
