---
title: FKs têm índice (o EF cria) — mas verifique FKs criadas por SQL cru e índices redundantes
impact: HIGH
impactDescription: 10-100x em JOIN/DELETE em cascata; evita lock longo ao deletar pai
tags: foreign-keys, indexes, cascade, ef-core
---

## FKs têm índice (o EF cria) — mas verifique FKs criadas por SQL cru e índices redundantes

O Postgres **não** indexa FK automaticamente; o EF Core sim (convenção `IX_<Tabela>_<Fk>`). Os furos aparecem quando a FK nasce em `migrationBuilder.Sql(...)` (expand/contract com `NOT VALID`), em tabela particionada, ou quando um índice composto torna o simples redundante.

**Incorreto (FK por SQL sem índice):**

```sql
ALTER TABLE "Agendamento" ADD CONSTRAINT "FK_Agendamento_Transportadora_TransportadoraId"
    FOREIGN KEY ("TransportadoraId") REFERENCES "Transportadora"("Id") NOT VALID;
-- DELETE FROM "Transportadora" → seq scan em "Agendamento" para checar referências
```

**Correto:**

```sql
CREATE INDEX CONCURRENTLY "IX_Agendamento_TransportadoraId" ON "Agendamento" ("TransportadoraId");
```

```csharp
builder.HasOne(x => x.Transportadora).WithMany().HasForeignKey(x => x.TransportadoraId)
    .OnDelete(DeleteBehavior.Restrict); // cascata em dado fiscal/operacional é perigosa
// HasIndex é implícito pela convenção — explicite se for composto
```

FKs sem índice:

```sql
SELECT c.conrelid::regclass AS tabela, a.attname AS coluna
FROM pg_constraint c
JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY(c.conkey)
WHERE c.contype = 'f'
  AND NOT EXISTS (SELECT 1 FROM pg_index i
                  WHERE i.indrelid = c.conrelid AND a.attnum = i.indkey[0]);
```

Índices nunca usados (candidatos a remover):

```sql
SELECT relname, indexrelname, idx_scan FROM pg_stat_user_indexes
WHERE idx_scan = 0 ORDER BY pg_relation_size(indexrelid) DESC;
```

**No TruckFlow:** `DeleteBehavior` — Agendamento/Grade são hard-deleted hoje; prefira `Restrict` para tudo que tem rastro fiscal (`NotaFiscal`, `RecebimentoEvento`, `AuditLog`).

Referência: [Use The Index, Luke — FK](https://use-the-index-luke.com/sql/where-clause/the-equals-operator/foreign-key) · [EF Core — Indexes by convention](https://learn.microsoft.com/ef/core/modeling/indexes#index-on-foreign-keys) · [Cybertec — FK indexes](https://www.cybertec-postgresql.com/en/index-your-foreign-key/)
