---
title: Busca textual em português com tsvector + unaccent (ou trigram para nomes curtos)
impact: LOW
impactDescription: 100x mais rápido que ILIKE '%x%' em texto longo; acentos ignorados
tags: full-text-search, tsvector, portuguese, unaccent, trigram
---

## Busca textual em português com tsvector + unaccent (ou trigram para nomes curtos)

O `search` das listagens com `ILIKE '%conceicao%'` não acha "Conceição" e faz seq scan. Para nomes curtos (fornecedor, produto, motorista), trigram + `unaccent` resolve (`query-index-types`). Para texto longo (observações, corpo de notificação, descrição de item de NF-e), full-text com dicionário `portuguese`.

**Incorreto:**

```csharp
q = q.Where(i => i.Descricao.ToLower().Contains(search.ToLower()));   // lower() na coluna: sem índice, sem acento
```

**Correto (coluna gerada + GIN):**

```sql
CREATE EXTENSION IF NOT EXISTS unaccent;
-- unaccent não é IMMUTABLE: wrapper para poder usar em coluna gerada/índice
CREATE FUNCTION immutable_unaccent(text) RETURNS text
    LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $$ SELECT public.unaccent('public.unaccent', $1) $$;

ALTER TABLE "NotaFiscalItem" ADD COLUMN "BuscaTsv" tsvector
    GENERATED ALWAYS AS (to_tsvector('portuguese', immutable_unaccent(coalesce("Descricao", '')))) STORED;
CREATE INDEX "IX_NotaFiscalItem_BuscaTsv" ON "NotaFiscalItem" USING gin ("BuscaTsv");

SELECT * FROM "NotaFiscalItem"
WHERE "EmpresaId" = $1 AND "BuscaTsv" @@ websearch_to_tsquery('portuguese', immutable_unaccent($2));
```

```csharp
// Npgsql EF: coluna NpgsqlTsVector + tradução de Matches
builder.Property(x => x.BuscaTsv).HasComputedColumnSql(
    "to_tsvector('portuguese', immutable_unaccent(coalesce(\"Descricao\", '')))", stored: true);
builder.HasIndex(x => x.BuscaTsv).HasMethod("gin");
q = q.Where(i => i.BuscaTsv.Matches(EF.Functions.WebSearchToTsQuery("portuguese", termo)));
```

Prefira trigram quando o usuário digita pedaço de palavra ("alim" → "Alimentos"); full-text quando digita palavras inteiras em texto longo.

**No TruckFlow:** `Search` em `GradeListQueryDto`, `UsuarioListQueryDto`, etc.; itens de NF-e (`NotaFiscalItem.Descricao`) e o `ProdutoLearningService` (sugestão de produto por descrição) são os candidatos.

Referência: [Full Text Search](https://www.postgresql.org/docs/16/textsearch.html) · [unaccent](https://www.postgresql.org/docs/16/unaccent.html) · [Npgsql — Full text search](https://www.npgsql.org/efcore/mapping/full-text-search.html)
