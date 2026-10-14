---
title: Efeito colateral vira evento de domínio → outbox, nunca chamada inline
impact: HIGH
impactDescription: notificação/push nunca "some" se o commit falhar e nunca sai se o commit não aconteceu
tags: domain-events, outbox, notifications, reliability, multi-instance
---

## Efeito colateral vira evento de domínio → outbox, nunca chamada inline

Chamar `INotificacaoService`/Expo/e-mail no meio do caso de uso cria o problema do *dual write*: o push sai e a transação faz rollback (motorista notificado de algo que não aconteceu), ou o commit sai e o push falha (ninguém fica sabendo). O TruckFlow já resolve com **transactional outbox**: a entidade chama `AddDomainEvent(...)`, o `OutboxSaveChangesInterceptor` grava `OutboxEvent` **no mesmo SaveChanges**, e o `OutboxProcessorWorker` (claim com `FOR UPDATE SKIP LOCKED`, retry exponencial até 8 tentativas) despacha para `IDomainEventHandler<T>` sob `WithTenant(EmpresaId)`.

**Incorreto:**

```csharp
agendamento.AlterarStatus(StatusAgendamento.Cancelado);
await _repo.Update(agendamento, ct);
await _notificacaoService.EnviarPushAsync(agendamento.UsuarioId, "Agendamento cancelado", ct); // dual write
```

**Correto:**

```csharp
// Domain/Events — record imutável com EmpresaId, OcorridoEm e IdempotencyKey
public sealed record AgendamentoCanceladoEvent(
    Guid AgendamentoId, Guid EmpresaId, Guid? MotoristaUsuarioId,
    string? MotivoCancelamento, DateTime OcorridoEm) : IDomainEvent { /* IdempotencyKey */ }

// Entidade
public void Cancelar(string? motivo = null)
{
    AlterarStatus(StatusAgendamento.Cancelado);
    AddDomainEvent(new AgendamentoCanceladoEvent(Id, EmpresaId, UsuarioId, motivo, DateTime.UtcNow));
}

// Application/Notificacoes/Handlers — idempotente
public sealed class AgendamentoCanceladoHandler(...) : IDomainEventHandler<AgendamentoCanceladoEvent>
{
    public async Task HandleAsync(AgendamentoCanceladoEvent evt, CancellationToken ct) { /* cria Notificacao + NotificacaoEntrega */ }
}
```

Handlers devem ser **idempotentes** (o outbox entrega *at-least-once*): use `IdempotencyKey` do evento ou unique constraint no efeito. Evento novo = registrar o tipo no `OutboxEventTypeResolver` + handler Scoped na extension `Notificacao`.

**No TruckFlow:** `EntidadeBase.DomainEvents`, `Infra/Database/Interceptors/OutboxSaveChangesInterceptor.cs`, `OutboxProcessorWorker`, `Docs/notificacoes/sse-implementacao.md`, ADR-0002.

Referência: [Chris Richardson — Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html) · [Milan Jovanović — Outbox pattern](https://www.milanjovanovic.tech/blog/outbox-pattern-for-reliable-microservices-messaging) · [Gregor Hohpe — Idempotent Receiver](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html) · [Microsoft — Domain events: design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)
