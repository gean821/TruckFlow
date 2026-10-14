---
title: Nada de fetch manual em onMounted/setInterval — use query
impact: CRITICAL
impactDescription: cache, dedupe, loading/erro e invalidação de graça
tags: vue-query, anti-pattern, onMounted, polling
---

## Nada de fetch manual em onMounted/setInterval — use query

**Impacto: CRITICAL (cache, dedupe, loading/erro e invalidação de graça)**

Fetch manual com `ref` + `onMounted` não participa do cache: a mutation de outra tela não atualiza esse componente, não há dedupe, e o `setInterval` vaza se esquecer o cleanup. A regra do repo é clara: **componentes consomem hooks/queries, nunca o service direto**.

**Incorreto (`components/dashboard/UpcomingAppointments.vue`, `views/RecebimentosOrfaosView.vue` — não copie):**

```ts
const service = AgendamentoService()
const items = ref<AgendamentoResponse[]>([])
const loading = ref(false)

async function load() {
  loading.value = true
  items.value = (await service.getByFilters({ pageNumber: 1, pageSize: 5 })).items
  loading.value = false
}
onMounted(() => { load(); setInterval(load, 120_000) }) // interval nunca limpo
```

**Correto:**

```ts
// src/queries/agendamento.queries.ts
export function useProximosAgendamentosQuery() {
  return useQuery({
    queryKey: [...agendamentoKeys.lists(), 'proximos'],
    queryFn: ({ signal }) => service.getByFilters({ pageNumber: 1, pageSize: 5 }, signal),
    refetchInterval: 120_000, // pausa sozinho com a aba em background
  })
}

// componente
const { data, isLoading } = useProximosAgendamentosQuery()
```

Exceção legítima: exportação CSV de "tudo" — mesmo assim, chame via função do hook (`useAgendamento().exportarCsv`) ou `queryClient.fetchQuery`, não o service direto na view.

Referência: [Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/queries) · [Why You Want React Query — TkDodo](https://tkdodo.eu/blog/why-you-want-react-query)
