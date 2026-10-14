---
title: Elimine N+1 — projeção com Select para leitura, Include só quando vai mutar, AsSplitQuery para várias coleções
impact: MEDIUM-HIGH
impactDescription: 1+N round-trips → 1-2 queries; payload 3-10x menor com projeção
tags: n-plus-one, include, projection, split-query, ef-core
---

## Elimine N+1 — projeção com Select para leitura, Include só quando vai mutar, AsSplitQuery para várias coleções

N+1 no EF aparece de três formas: lazy loading (desligado no TruckFlow — ótimo), loop com query dentro, e acesso a navegação não carregada que "funciona" retornando null. O remédio certo depende do uso.

**Incorreto:**

```csharp
var agendamentos = await _db.Agendamento.Where(...).ToListAsync(ct);
foreach (var a in agendamentos)
{
    a.Fornecedor = await _db.Fornecedor.FindAsync(a.FornecedorId);   // 1 query por linha
}
// ou: Include de tudo para montar uma tela de listagem
var lista = await _db.Agendamento
    .Include(x => x.Fornecedor).Include(x => x.Usuario).ThenInclude(u => u.Motorista)
    .Include(x => x.NotaFiscal).ThenInclude(n => n.Itens)              // cartesian explosion
    .Include(x => x.Grade).ThenInclude(g => g.Produto)
    .ToListAsync(ct);
```

**Correto (listagem = projeção para o DTO):**

```csharp
var items = await query
    .OrderByDescending(x => x.DataInicio)
    .Skip((page - 1) * size).Take(size)
    .Select(a => new AgendamentoAdminResponse
    {
        Id = a.Id,
        FornecedorNome = a.Fornecedor!.Nome,
        MotoristaNome = a.Usuario!.Motorista!.NomeReal,
        LocalDescarga = a.LocalDescarga!.Nome,
        Status = a.StatusAgendamento.ToString(),
        DataInicio = a.DataInicio,
    })
    .ToListAsync(ct);   // 1 query, só as colunas da tela, sem tracking
```

```sql
SELECT a."Id", f."Nome", m."NomeReal", l."Nome", a."StatusAgendamento", a."DataInicio"
FROM "Agendamento" a
LEFT JOIN "Fornecedor" f ON f."Id" = a."FornecedorId"
LEFT JOIN "AspNetUsers" u ON u."Id" = a."UsuarioId"
LEFT JOIN "Motorista" m ON m."UsuarioId" = u."Id"
LEFT JOIN "LocalDescarga" l ON l."Id" = a."LocalDescargaId"
WHERE a."EmpresaId" = $1 ORDER BY a."DataInicio" DESC LIMIT $2 OFFSET $3;
```

Quando usar cada um:
- **`Select` (projeção)**: toda leitura para tela/listagem. Preferido.
- **`Include`**: quando vai **modificar** o agregado e salvar (tracking).
- **`AsSplitQuery()`**: Include de 2+ coleções (`NotaFiscal.Itens` + outra) — evita produto cartesiano; cuidado: sem snapshot consistente entre as queries.
- Lookup em lote: `Where(f => ids.Contains(f.Id))` → `= ANY($1)`, uma query.

**No TruckFlow:** `AgendamentoRepositorio.GetAdminViewAsync` já projeta direto (bom para performance — dívida de camada é outra discussão); `GradeRepositorio.GetPagedAsync` carrega entidades e o service faz `MapToResponse` (ok para tabela pequena, revisar se crescer).

Referência: [EF Core — Efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying) · [EF Core — Single vs split queries](https://learn.microsoft.com/ef/core/querying/single-split-queries)
