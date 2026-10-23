---
title: Assinatura honesta — FirstOrDefault retorna Task<T?>, sem "!" para calar o compilador
impact: HIGH
impactDescription: NullReferenceException 500 vira NotFoundException 404 com mensagem
tags: nullable-reference-types, repository, ef-core
---

## Assinatura honesta — FirstOrDefault retorna Task<T?>, sem "!" para calar o compilador

Os projetos têm `<Nullable>enable</Nullable>`. O valor disso é o compilador obrigar o service a tratar o "não achou". `return agendamento!;` numa assinatura `Task<Agendamento>` mente para o compilador: o service não faz `?? throw`, o null escapa, e o erro aparece 3 linhas depois como `NullReferenceException` → 500.

**Incorreto:**

```csharp
public async Task<Agendamento> GetById(Guid id, CancellationToken token = default)
{
    var agendamento = await _db.Agendamento.Include(...).FirstOrDefaultAsync(x => x.Id == id, token);
    return agendamento!;          // pode ser null
}
```

**Correto:**

```csharp
public Task<Agendamento?> GetById(Guid id, CancellationToken ct) =>
    _db.Agendamento
        .Include(x => x.Fornecedor)
        .Include(x => x.LocalDescarga)
        .FirstOrDefaultAsync(x => x.Id == id, ct);

// service
var agendamento = await _repo.GetById(id, ct)
    ?? throw new NotFoundException("Agendamento não encontrado.");
```

`!` só é aceitável em expressão projetada (`x.Fornecedor!.Nome` dentro de `Select`, que vira LEFT JOIN) e em navegação garantida por `Include` + FK obrigatória — com o motivo óbvio no contexto. Ao corrigir a assinatura de um método existente, ajuste os chamadores (o compilador lista) — mudança pequena e segura.

**No TruckFlow:** dívida em `IAgendamentoRepositorio.GetById` / `AgendamentoRepositorio.GetById`.

Referência: [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references) · [EF Core — Working with nullable reference types](https://learn.microsoft.com/ef/core/miscellaneous/nullable-reference-types)
