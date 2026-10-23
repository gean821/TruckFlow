---
title: Leitura que não vai ser alterada usa AsNoTracking (ou projeção)
impact: MEDIUM
impactDescription: menos memória e CPU por request em listagens e dashboards
tags: ef-core, tracking, performance, read
---

## Leitura que não vai ser alterada usa AsNoTracking (ou projeção)

Change tracking custa snapshot de cada entidade carregada. Em endpoint de leitura (listagem, dashboard, relatório, `GetById` só para exibir), use `.AsNoTracking()` — ou projete com `Select`, que já não rastreia. **Não** use `AsNoTracking` em query cuja entidade vai receber método de domínio e `SaveChanges` (a alteração não seria detectada; e com `xmin` você perde a checagem de concorrência).

**Incorreto:**

```csharp
public Task<List<Produto>> ListarParaDropdown(CancellationToken ct) =>
    _db.Produto.Include(p => p.LocalDescarga).ToListAsync(ct);   // rastreia tudo para só exibir
```

**Correto:**

```csharp
public Task<List<ProdutoOpcaoResponse>> ListarParaDropdown(CancellationToken ct) =>
    _db.Produto
        .OrderBy(p => p.Nome)
        .Select(p => new ProdutoOpcaoResponse { Id = p.Id, Nome = p.Nome })
        .ToListAsync(ct);

public Task<Grade?> GetParaExibir(Guid id, CancellationToken ct) =>
    _db.Grade.AsNoTracking().Include(g => g.Produto).FirstOrDefaultAsync(g => g.Id == id, ct);
```

O global query filter de tenant continua valendo com `AsNoTracking`. Se o mesmo método de repositório serve leitura e escrita, separe em dois (`GetById` rastreado para escrita, `GetParaExibir`/projeção para leitura) em vez de um parâmetro `bool tracking`.

**No TruckFlow:** `DashboardRepositorio` e listagens são candidatos naturais. Para N+1 e Include vs split query, ver `postgres-best-practices/data-n-plus-one`.

Referência: [EF Core — Tracking vs. No-Tracking Queries](https://learn.microsoft.com/ef/core/querying/tracking) · [EF Core — Performance: efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying)
