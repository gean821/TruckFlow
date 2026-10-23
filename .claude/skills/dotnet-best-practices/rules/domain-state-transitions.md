---
title: Transição de estado mora na entidade, validada por regra central
impact: HIGH
impactDescription: nenhum caminho de código consegue levar um agendamento Finalizado de volta a Disponivel
tags: ddd, domain-model, state-machine, invariants
---

## Transição de estado mora na entidade, validada por regra central

O modelo do TruckFlow é híbrido (setters públicos por causa do EF, mas comportamento nas entidades). A regra que **não** é negociável: status muda só por método de domínio (`AlterarStatus`, `Reservar`, `Cancelar`, `Reagendar`, `Expirar`), que consulta a tabela de transições em `Domain/Rules/StatusAgendamentoRules.cs` (`PodeTransitarPara`) e lança `DomainException` se inválida. Service que faz `agendamento.StatusAgendamento = ...` pula a invariante e o evento de domínio.

**Incorreto (Transaction Script atropelando o domínio):**

```csharp
agendamento.StatusAgendamento = StatusAgendamento.Cancelado;   // sem validação, sem evento
agendamento.UpdatedAt = DateTime.UtcNow;
await _repo.Update(agendamento, ct);
```

**Correto:**

```csharp
// Domain/Entities/Agendamento.cs
public void AlterarStatus(StatusAgendamento novoStatus)
{
    if (!StatusAgendamento.PodeTransitarPara(novoStatus))
        throw new DomainException($"Transição inválida: {StatusAgendamento} → {novoStatus}");
    StatusAgendamento = novoStatus;
    UpdatedAt = DateTime.UtcNow;
}

public void Cancelar(string? motivo = null)
{
    AlterarStatus(StatusAgendamento.Cancelado);
    AddDomainEvent(new AgendamentoCanceladoEvent(
        AgendamentoId: Id, EmpresaId: EmpresaId, MotoristaUsuarioId: UsuarioId,
        MotivoCancelamento: motivo, OcorridoEm: DateTime.UtcNow));
}

// Service
agendamento.Cancelar(dto.Motivo);
await _repo.Update(agendamento, ct);
```

Nova transição = editar **só** `StatusAgendamentoRules` + teste de domínio cobrindo a matriz Allowed/Denied (ADR-0004 item 14). Mensagem de `DomainException` chega ao usuário — escreva legível.

Caminho de evolução (sem quebrar EF): `private set` em `StatusAgendamento` (EF Core materializa por backing field/setter privado) para o compilador impedir atribuição direta. Faça por entidade, com a suíte verde.

**No TruckFlow:** `Agendamento.AlterarStatus/Cancelar/Reservar/Expirar`, `Grade.GerarSlots()`, `RecebimentoEvento.CriarOrfao(...)`.

Referência: [Martin Fowler — Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html) · [Vladimir Khorikov — Always-valid domain model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/) · Eric Evans, *Domain-Driven Design* (2003) · [EF Core — Backing fields](https://learn.microsoft.com/ef/core/modeling/backing-field)
