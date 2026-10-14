---
title: Conflito de concorrência merece 409, não 400 (evolução)
impact: MEDIUM
impactDescription: cliente distingue "dado inválido" de "alguém chegou antes" e reage (refetch automático)
tags: http-status, concurrency, conflict, api-design
---

## Conflito de concorrência merece 409, não 400 (evolução)

Hoje toda corrida (xmin, 23505, 23P01) sai como `BusinessException` → 400. Funciona, mas o front/mobile não consegue diferenciar "você digitou errado" de "a vaga acabou de ser pega" — e no segundo caso o certo é **refazer a query** automaticamente. Uma `ConflictException` mapeada para **409** com `code` próprio resolve sem quebrar quem só lê `message`.

**Incorreto (tudo é 400):**

```csharp
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista.");
}
```

**Correto:**

```csharp
public class ConflictException(string message)
    : HttpResponseException("CONFLICT", message) { }

// middleware
ConflictException => StatusCodes.Status409Conflict,

catch (DbUpdateConcurrencyException)
{
    throw new ConflictException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

Cliente (Vue Query / React Query): `onError` com `status === 409` → `invalidateQueries` da lista + toast.

Introduza por endpoint, começando por `/reservar`, e avise os agentes de front/mobile na mesma PR.

**No TruckFlow:** candidato natural em `AgendamentoMotoristaService.BookAppointment` e `AgendamentoAdminService`.

Referência: [RFC 9110 §15.5.10 — 409 Conflict](https://www.rfc-editor.org/rfc/rfc9110#name-409-conflict) · [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)
