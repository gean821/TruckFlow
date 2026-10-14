---
title: Paginação offset (PagedResponse) para telas; keyset para feeds/exports grandes
impact: MEDIUM-HIGH
impactDescription: keyset mantém O(page) em páginas profundas; offset degrada linearmente
tags: pagination, offset, keyset, cursor, count
---

## Paginação offset (PagedResponse) para telas; keyset para feeds/exports grandes

Convenção do TruckFlow: **toda listagem de tela é paginada server-side** (`*ListQueryDto` + `PagedResponse<T>` com `TotalCount`/`TotalPages`). Isso usa `OFFSET` + `COUNT(*)` — ótimo para UI com número de página. Mas `OFFSET 50000` lê e descarta 50k linhas, e `COUNT(*)` varre tudo que bate no filtro.

**Incorreto (offset para export/feed grande, sem ordenação estável):**

```csharp
// export CSV de 1000 em 1000 com offset → cada página fica mais lenta
for (var page = 1; ; page++)
{
    var lote = await q.Skip((page - 1) * 1000).Take(1000).ToListAsync(ct); // sem OrderBy → páginas inconsistentes
}
```

**Correto — tela (padrão atual, com ordenação determinística):**

```csharp
var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
var pageSize = Math.Clamp(query.PageSize, 1, 100);                  // limite superior!
var totalCount = await dbQuery.CountAsync(ct);
var items = await dbQuery
    .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)          // desempate estável
    .Skip((pageNumber - 1) * pageSize).Take(pageSize)
    .Select(...).ToListAsync(ct);
```

**Correto — keyset (feed infinito, export, sincronização mobile):**

```sql
SELECT * FROM "Notificacao"
WHERE "EmpresaId" = $1 AND "DestinatarioUsuarioId" = $2
  AND ("CreatedAt", "Id") < ($3, $4)                 -- cursor da última linha vista
ORDER BY "CreatedAt" DESC, "Id" DESC
LIMIT 20;
-- usa o índice (EmpresaId, DestinatarioUsuarioId, CreatedAt) direto, em qualquer profundidade
```

```csharp
q = q.Where(n => n.CreatedAt < cursor.CreatedAt
              || (n.CreatedAt == cursor.CreatedAt && n.Id.CompareTo(cursor.Id) < 0))
     .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(20);
```

Sinais para trocar para keyset: export (`pageSize: 1000` no front), lista de notificações do mobile com scroll infinito, tabelas com >100k linhas por tenant. Sempre limite `PageSize` no backend (o front já pediu 1000 em export).

**No TruckFlow:** padrão em `GradeRepositorio.GetPagedAsync`, `PlanejamentoListQueryDto` + `PlanejamentoRecebimentoService.GetPaged`. Export de agendamentos no front (`VisualizarAgendamentoView`, `pageSize: 1000`).

Referência: [Use The Index, Luke — No Offset](https://use-the-index-luke.com/no-offset) · [Markus Winand — Paging Through Results](https://use-the-index-luke.com/sql/partial-results/fetch-next-page) · [EF Core — Pagination](https://learn.microsoft.com/ef/core/querying/pagination)
