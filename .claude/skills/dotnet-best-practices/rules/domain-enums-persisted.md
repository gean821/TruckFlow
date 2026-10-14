---
title: Enum persistido tem valor int explícito e nunca é reordenado
impact: HIGH
impactDescription: evita corromper status de milhares de agendamentos com um "reorganizar enum"
tags: enum, persistence, migration, contract
---

## Enum persistido tem valor int explícito e nunca é reordenado

Enums do TruckFlow são gravados como `int` e aparecem em SQL cru (a exclusion constraint filtra `"StatusAgendamento" IN (0, 1, 2, 3)`), em payloads do mobile (`PlataformaDispositivo { Ios = 1, Android = 2 }`) e em eventos do outbox já gravados. Reordenar ou inserir no meio muda o significado de dados existentes **sem erro nenhum**.

Regras:
- Todo membro com valor explícito.
- Novo valor sempre no **fim**, com número novo. Nunca reutilizar número removido.
- Remover membro = marcar `[Obsolete]` e manter o número.
- Se o valor aparece em SQL (constraint, índice parcial, view), a migration que adiciona o membro revisa esse SQL.
- Para o front, o DTO expõe `.ToString()` (nome) quando a tela exibe; enum numérico só onde o cliente espelha o enum.

**Incorreto:**

```csharp
public enum StatusAgendamento
{
    Disponivel, Pendente, Agendado, Confirmado /* inserido no meio */, EmAndamento, Finalizado, Cancelado, Expirado
}
```

**Correto:**

```csharp
public enum StatusAgendamento
{
    Disponivel = 0,
    Pendente = 1,
    Agendado = 2,
    EmAndamento = 3,
    Finalizado = 4,
    Cancelado = 5,
    Expirado = 6,
    NoShow = 7,        // novo: no fim — e revisar agendamento_no_overlap_per_doca
}
```

**No TruckFlow:** `TruckFlow.Domain/Enums/*` já usa valores explícitos — mantenha. Front (`src/enums/StatusAgendamento`) e mobile (`src/enums/AgendamentoStatus`) espelham: atualizar os três na mesma feature.

Referência: [EF Core — Value conversions (enums)](https://learn.microsoft.com/ef/core/modeling/value-conversions) · [.NET — Enum design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/enum)
