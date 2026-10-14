---
title: Evite waterfall de queries dependentes desnecessárias
impact: CRITICAL
impactDescription: elimina um round-trip inteiro por nível de dependência
tags: async, waterfalls, vue-query, dependent-queries
---

## Evite waterfall de queries dependentes desnecessárias

**Impacto: CRITICAL (elimina um round-trip inteiro por nível de dependência)**

Queries com `enabled` encadeado (A → B → C) carregam em série. Só encadeie quando B realmente precisa de um dado que **só** A tem. Se o id já está na rota, dispare tudo em paralelo.

**Incorreto (conferência espera o agendamento só para ler um id que já está na URL):**

```ts
const agendamentoId = computed(() => route.params.agendamentoId as string)
const { data: agendamento } = useAgendamentoByIdQuery(agendamentoId)
const conferenciaId = computed(() => agendamento.value?.id)
const { data: conferencia } = useConferenciaQuery(conferenciaId) // espera o primeiro
```

**Correto (as duas queries partem juntas):**

```ts
const agendamentoId = computed(() => route.params.agendamentoId as string)
const { data: agendamento } = useAgendamentoByIdQuery(agendamentoId)
const { data: conferencia } = useConferenciaQuery(agendamentoId)
```

Se a tela sempre precisa de A+B juntos, considere um endpoint agregado no backend (`GET /agendamentos/{id}/conferencia-completa`) — um round-trip, uma transação, um filtro de tenant.

Referência: [Dependent Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/dependent-queries) · [Parallel Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/parallel-queries)
