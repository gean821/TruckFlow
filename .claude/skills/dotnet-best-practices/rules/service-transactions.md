---
title: Transação explícita só quando há mais de um SaveChanges — curta e com await using
impact: HIGH
impactDescription: reserva + evento de recebimento comitam juntos ou nenhum comita
tags: transaction, ef-core, unit-of-work, consistency
---

## Transação explícita só quando há mais de um SaveChanges — curta e com await using

Um único `SaveChangesAsync` já é atômico. Como os repositórios do TruckFlow chamam `SaveChangesAsync` por conta própria, **dois** métodos de repositório no mesmo caso de uso são dois commits — se o segundo falhar, o primeiro fica. Nesse caso abra transação via `_repo.BeginTransactionAsync(ct)` com `await using` (rollback automático no dispose se não houver commit).

Regras:
- Nada de I/O externo dentro da transação (SEFAZ, Expo, e-mail, geocoding) — segura lock e conexão. Efeito externo vai para o **outbox** (evento de domínio) e roda depois do commit.
- Transação curta: carregue o que precisa antes, abra, escreva, comite.
- Os `catch` de tradução de erro Postgres ficam **em volta** do commit (ver `error-map-postgres-sqlstate`).

**Incorreto:**

```csharp
await _repo.Update(vaga, ct);                         // commit 1
await _lifecycle.AoReservarAsync(vaga, peso, ct);     // commit 2 — se falhar, vaga reservada sem evento
await _expo.SendAsync(push, ct);                      // HTTP externo no meio do fluxo
```

**Correto:**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
try
{
    await _repo.Update(vaga, ct);
    await _lifecycle.AoReservarAsync(vaga, pesoEstimado, ct);
    await tx.CommitAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
// push: vaga.Reservar(...) adicionou evento → OutboxSaveChangesInterceptor → PushDispatcherWorker
```

Evolução de design (registrar em ADR se for adotar): repositórios que **só fazem staging** e um `IUnitOfWork.SaveChangesAsync` no service eliminam a necessidade de transação explícita na maioria dos casos (Fowler, PoEAA — Unit of Work). Hoje o projeto mistura os dois estilos; em agregado novo, escolha um e seja consistente nele.

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment`, `AgendamentoAdminService` (Create/Update/Cancelar/Finalizar).

Referência: [EF Core — Transactions](https://learn.microsoft.com/ef/core/saving/transactions) · [Martin Fowler — Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html) · [Milan Jovanović — Outbox pattern](https://www.milanjovanovic.tech/blog/outbox-pattern-for-reliable-microservices-messaging)
