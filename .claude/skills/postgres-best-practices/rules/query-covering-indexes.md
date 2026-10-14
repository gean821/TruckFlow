---
title: Use covering index (INCLUDE) para listagens e contagens quentes
impact: MEDIUM-HIGH
impactDescription: 2-5x eliminando heap fetches (index-only scan)
tags: indexes, covering-index, include, index-only-scan
---

## Use covering index (INCLUDE) para listagens e contagens quentes

Quando a query só lê colunas presentes no índice, o Postgres faz **index-only scan** sem tocar a tabela. `INCLUDE` adiciona colunas ao índice sem fazê-las parte da chave.

**Incorreto (index scan + heap fetch por linha):**

```sql
-- Badge de "não lidas": índice acha as linhas, mas busca cada uma na tabela
SELECT count(*) FROM "Notificacao"
WHERE "EmpresaId" = @e AND "DestinatarioUsuarioId" = @u AND "LidaEm" IS NULL;
```

**Correto (o índice responde sozinho):**

```sql
CREATE INDEX "IX_Notificacao_NaoLidas"
    ON "Notificacao" ("EmpresaId", "DestinatarioUsuarioId")
    INCLUDE ("Titulo", "Prioridade", "CreatedAt")
    WHERE "LidaEm" IS NULL;
```

```csharp
// Npgsql EF Core: IncludeProperties
builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId })
    .IncludeProperties(x => new { x.Titulo, x.Prioridade, x.CreatedAt })
    .HasFilter("\"LidaEm\" IS NULL");
```

E a query precisa **projetar** só essas colunas (`.Select(n => new { n.Titulo, n.Prioridade, n.CreatedAt })`) — `SELECT *` via entidade completa anula o benefício.

Index-only scan depende do visibility map estar em dia (VACUUM): veja `Heap Fetches` no `EXPLAIN (ANALYZE)`. Em tabela com muito UPDATE o ganho cai.

**No TruckFlow:** `NotificacaoConfiguracao.cs` já tem o índice parcial `(EmpresaId, DestinatarioUsuarioId) WHERE "LidaEm" IS NULL` usado pelo `unread-count` (polling + `refetchOnWindowFocus` no front e mobile — é query frequente).

Referência: [Index-Only Scans and Covering Indexes](https://www.postgresql.org/docs/16/indexes-index-only-scans.html) · [Npgsql — Index include](https://www.npgsql.org/efcore/modeling/indexes.html)
