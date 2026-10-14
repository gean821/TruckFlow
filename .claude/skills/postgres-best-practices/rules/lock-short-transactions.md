---
title: Transações curtas — nada de I/O externo (SEFAZ, Expo, e-mail) dentro de transação
impact: HIGH
impactDescription: locks segurados por ms em vez de segundos; sem cascata de espera
tags: transactions, locks, external-calls, outbox
---

## Transações curtas — nada de I/O externo (SEFAZ, Expo, e-mail) dentro de transação

Lock de linha dura até o `COMMIT`. Uma chamada à SEFAZ (timeout de 60s no `ZeusSefazClient`) dentro da transação segura a linha do agendamento — qualquer outra operação na mesma vaga espera, o pool esvazia, e um timeout de rede vira incidente no banco.

**Incorreto:**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
var vaga = await _repo.GetById(id, ct);
var status = await _sefaz.ConsultarProtocoloAsync(chave, uf, ct);   // até 60s com lock aberto
await _email.EnviarAsync(...);                                        // mais I/O externo
vaga.Reservar(...);
await _repo.Update(vaga, ct);
await tx.CommitAsync(ct);
```

**Correto (I/O antes; efeito colateral depois via outbox):**

```csharp
var status = await _sefaz.ConsultarProtocoloAsync(chave, uf, ct);    // fora da transação
await using var tx = await _repo.BeginTransactionAsync(ct);
var vaga = await _repo.GetById(id, ct);
vaga.Reservar(...);                     // levanta domain event → OutboxSaveChangesInterceptor grava OutboxEvent
await _repo.Update(vaga, ct);           // mesma transação: estado + evento atômicos
await tx.CommitAsync(ct);
// push/e-mail saem pelo OutboxProcessorWorker depois do commit
```

```sql
-- transações longas agora
SELECT pid, now() - xact_start AS duracao, state, left(query, 80)
FROM pg_stat_activity WHERE xact_start IS NOT NULL ORDER BY duracao DESC;
```

Transactional outbox é exatamente para isso: o "enviar notificação" é gravado junto com a mudança de estado e executado fora. Nunca chame `IExpoPushClient` direto de dentro de um service transacional.

**No TruckFlow:** `EntidadeBase.AddDomainEvent` + `OutboxSaveChangesInterceptor` + `OutboxProcessorWorker` (ADR-0002). `BookAppointment` já faz a transação só com `Update` + lifecycle.

Referência: [Chris Richardson — Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html) · [Explicit Locking](https://www.postgresql.org/docs/16/explicit-locking.html)
