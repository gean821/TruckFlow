---
title: Leitura que não vai salvar usa AsNoTracking (ou projeção)
impact: MEDIUM
impactDescription: 30-50% menos CPU/memória em listagens; sem identity map inflado
tags: asnotracking, change-tracking, ef-core, read-only
---

## Leitura que não vai salvar usa AsNoTracking (ou projeção)

Com tracking, o EF guarda snapshot de cada entidade para detectar mudanças no `SaveChanges`. Em listagem de 100 agendamentos com Includes, isso é memória e CPU desperdiçadas — e o risco de um `SaveChanges` posterior no mesmo contexto gravar algo que ninguém pretendia.

**Incorreto:**

```csharp
public async Task<PagedResponse<Grade>> GetPagedAsync(GradeListQueryDto q, CancellationToken ct)
{
    var items = await _db.Grade.Include(g => g.Produto)
        .OrderByDescending(x => x.CreatedAt).Skip(...).Take(...)
        .ToListAsync(ct);                       // tracked, só para mapear em DTO
}
```

**Correto:**

```csharp
var items = await _db.Grade.AsNoTracking()
    .Include(g => g.Produto)
    .OrderByDescending(x => x.CreatedAt).Skip(...).Take(...)
    .ToListAsync(ct);
// ou, melhor ainda, .Select(g => new GradeResponse { ... }) — projeção nunca é rastreada
```

`AsNoTracking` não muda o SQL; o ganho é no cliente. Os **filtros globais continuam valendo** (tenant protegido). `AsNoTrackingWithIdentityResolution()` quando o grafo tem a mesma entidade repetida. Não ligue `QueryTrackingBehavior.NoTracking` global: services de escrita do TruckFlow carregam e salvam no mesmo fluxo (`GetById` → método de domínio → `Update`).

**No TruckFlow:** repositórios de leitura paginada (`GradeRepositorio`, `AuditLogRepositorio`, `NotificacaoRepositorio`) são os candidatos; `GetById` usado antes de mutação deve continuar tracked.

Referência: [EF Core — Tracking vs No-Tracking](https://learn.microsoft.com/ef/core/querying/tracking)
