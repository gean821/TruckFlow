---
title: Escolha o tipo de índice certo (B-tree, GIN, GiST, BRIN)
impact: HIGH
impactDescription: 10-100x com o tipo adequado ao operador
tags: indexes, btree, gin, gist, brin, trigram
---

## Escolha o tipo de índice certo (B-tree, GIN, GiST, BRIN)

B-tree (default) serve `=`, `<`, `>`, `BETWEEN`, `ORDER BY` e `LIKE 'abc%'`. Não serve `ILIKE '%abc%'`, operadores de `jsonb` (`@>`, `?`) nem sobreposição de intervalo (`&&`).

**Incorreto (B-tree para busca "contém"):**

```sql
CREATE INDEX "IX_Fornecedor_Nome" ON "Fornecedor" ("Nome");
SELECT * FROM "Fornecedor" WHERE "EmpresaId" = @e AND "Nome" ILIKE '%alimentos%'; -- seq scan
```

```csharp
.Where(f => EF.Functions.ILike(f.Nome, $"%{search}%"))
```

**Correto (trigram GIN para busca parcial):**

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX "IX_Fornecedor_Nome_trgm" ON "Fornecedor" USING gin ("Nome" gin_trgm_ops);
```

```csharp
// Configuração Npgsql
builder.HasIndex(x => x.Nome).HasMethod("gin").HasOperators("gin_trgm_ops");
// e na migration: migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
```

Guia rápido:

| Tipo | Use para | No TruckFlow |
|---|---|---|
| B-tree | igualdade, intervalo, ordenação | quase tudo |
| GIN | `jsonb` (`@>`), arrays, trigram (`ILIKE '%x%'`), full-text | `Empresa.Configuracoes`, busca de fornecedor/produto |
| GiST | intervalos (`tstzrange &&`), exclusion constraint | `agendamento_no_overlap_per_doca` |
| BRIN | colunas de tempo em tabela append-only enorme | candidato para `MotoristaPosicaoHistorico."CapturadoEm"` |

**No TruckFlow:** a busca `search` das listagens paginadas (`*ListQueryDto.Search`) usa `ILIKE`/`Contains`; com milhares de linhas por tenant, avalie trigram antes de reclamar do debounce do front.

Referência: [Index Types](https://www.postgresql.org/docs/16/indexes-types.html) · [pg_trgm](https://www.postgresql.org/docs/16/pgtrgm.html) · [Npgsql — Index method](https://www.npgsql.org/efcore/modeling/indexes.html)
