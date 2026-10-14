---
title: Leitura paginada projeta direto para o DTO com Select
impact: HIGH
impactDescription: busca só as colunas da tela; elimina Include em cascata e tracking em listagens
tags: ef-core, projection, select, performance, pagination
---

## Leitura paginada projeta direto para o DTO com Select

Para **escrita**, carregue a entidade (com os `Include` necessários) — você vai chamar método de domínio nela. Para **listagem**, projete: `.Select(x => new XResponse { ... })` traduz para um `SELECT` só das colunas usadas, com `JOIN` para as navegações acessadas, sem tracking e sem materializar o grafo. Numa listagem de agendamentos com 6 `Include`, a diferença é grande em I/O e memória.

**Incorreto (listagem carregando o grafo inteiro):**

```csharp
var items = await _db.Agendamento
    .Include(x => x.Fornecedor).Include(x => x.Usuario).ThenInclude(u => u!.Motorista)
    .Include(x => x.NotaFiscal).Include(x => x.LocalDescarga).Include(x => x.Grade).ThenInclude(g => g!.Produto)
    .Skip(skip).Take(pageSize)
    .ToListAsync(ct);                    // tracking + todas as colunas de 7 tabelas
return items.Select(MapToResponse).ToList();
```

**Correto:**

```csharp
var items = await dbQuery
    .OrderBy(x => x.DataInicio).ThenBy(x => x.Id)
    .Skip(skip).Take(pageSize)
    .Select(x => new AgendamentoAdminResponse
    {
        Id = x.Id,
        FornecedorNome = x.Fornecedor!.Nome,
        MotoristaNome = x.Usuario!.Motorista!.NomeReal,
        PlacaVeiculo = x.PlacaVeiculo ?? x.NotaFiscal!.PlacaVeiculo,
        LocalDescarga = x.LocalDescarga!.Nome,
        DataInicio = x.DataInicio,
        Status = x.StatusAgendamento.ToString(),
    })
    .ToListAsync(ct);
```

Dentro de expressão projetada, `x.Fornecedor!.Nome` vira `LEFT JOIN` e devolve `null` sem exceção — o `!` só cala o compilador. Não chame métodos C# arbitrários dentro do `Select` (avaliação no cliente ou erro de tradução).

Trade-off conhecido: a projeção no repositório faz o repositório depender do DTO do Domain. Aceito no projeto por performance (`AgendamentoRepositorio.GetAdminViewAsync`). Alternativa mais limpa quando o mapeamento crescer: o repositório expõe `IQueryable<T>` filtrado e o service projeta — decida por entidade, não misture no mesmo agregado.

**No TruckFlow:** `AgendamentoRepositorio.GetAdminViewAsync` projeta; `GradeRepositorio.GetPagedAsync` materializa entidade e mapeia depois (ok para volume pequeno).

Referência: [EF Core — Efficient querying (project only properties you need)](https://learn.microsoft.com/ef/core/performance/efficient-querying#project-only-properties-you-need) · [Jon P Smith — EF Core in Action, cap. "Performance tuning"](https://www.thereformedprogrammer.net/)
