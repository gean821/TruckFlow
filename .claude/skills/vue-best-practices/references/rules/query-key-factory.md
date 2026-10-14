---
title: Query key factory por entidade
impact: HIGH
impactDescription: invalidação previsível de lista + detalhe, sem string solta
tags: vue-query, query-key, invalidation, tkdodo
---

## Query key factory por entidade

**Impacto: HIGH (invalidação previsível de lista + detalhe, sem string solta)**

Hoje o repo usa uma constante string (`fornecedorQueryKey = 'fornecedores'`) e, em alguns módulos, uma key **diferente** para o detalhe (`["agendamento", id]` vs `["agendamentos"]`). Resultado: a mutation invalida a lista e o detalhe fica velho. A solução, popularizada por TkDodo (*Effective React Query Keys*, vale 1:1 para Vue Query), é uma factory hierárquica por entidade.

**Incorreto (keys divergentes — `src/hooks/useAgendamento.ts`, não copie):**

```ts
// queries: ["agendamentos", filtros] e ["agendamento", id]
onSuccess: () => queryClient.invalidateQueries({ queryKey: ["agendamentos"] })
// o detalhe ["agendamento", id] NÃO é invalidado
```

**Correto (factory hierárquica):**

```ts
// src/queries/agendamento.queries.ts
export const agendamentoKeys = {
  all: ['agendamentos'] as const,
  lists: () => [...agendamentoKeys.all, 'list'] as const,
  list: (params: MaybeRef<IAgendamentoFilterDto>) => [...agendamentoKeys.lists(), params] as const,
  details: () => [...agendamentoKeys.all, 'detail'] as const,
  detail: (id: MaybeRef<string | null | undefined>) => [...agendamentoKeys.details(), id] as const,
}

// mutation: uma invalidação cobre lista E detalhe
onSuccess: () => queryClient.invalidateQueries({ queryKey: agendamentoKeys.all })
```

Ao migrar um módulo, mantenha o primeiro segmento igual ao atual (`'agendamentos'`) para não quebrar invalidações feitas por outros hooks/views (ex.: `GradeView` invalida `['agendamentos']`).

Referência: [Effective React Query Keys — TkDodo](https://tkdodo.eu/blog/effective-react-query-keys) · [Query Keys](https://tanstack.com/query/latest/docs/framework/vue/guides/query-keys)
