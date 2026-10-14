---
title: useUrlQueryState — filtros e página sincronizados com route.query
impact: MEDIUM-HIGH
impactDescription: filtro/paginação sobrevive a refresh e link compartilhado, sem copiar código
tags: composable, router, query-params, pagination
---

## useUrlQueryState — filtros e página sincronizados com route.query

**Impacto: MEDIUM-HIGH (filtro/paginação sobrevive a refresh e link compartilhado, sem copiar código)**

Convenção do projeto: listagens refletem filtros e página em `route.query`. Hoje cada view reimplementa: lê `route.query` no init, `watch` para `router.replace`, reset da página quando filtro muda. Extraia.

**Incorreto (copiado em cada listagem):**

```ts
const page = ref(Number(route.query.page) || 1)
const search = ref(route.query.search?.toString() ?? "")
watch([searchDebounced, filtroStatus], () => { page.value = 1 })
watch([searchDebounced, filtroStatus, page], () => {
  router.replace({ query: { search: searchDebounced.value || undefined, page: page.value !== 1 ? String(page.value) : undefined } })
})
```

**Correto:**

```ts
// src/composables/useListQueryState.ts
import { computed, ref, watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDebouncedRef } from './useDebouncedRef'

export function useListQueryState<F extends Record<string, string | undefined>>(defaults: F, pageSize = 12) {
  const route = useRoute()
  const router = useRouter()

  const page = ref(Number(route.query.page) || 1)
  const search = ref(route.query.search?.toString() ?? '')
  const searchDebounced = useDebouncedRef(search)
  const filters = ref({ ...defaults, ...pick(route.query, Object.keys(defaults)) }) as Ref<F>

  watch([searchDebounced, filters], () => { page.value = 1 }, { deep: true })
  watch([searchDebounced, filters, page], () => {
    router.replace({
      query: {
        ...clean(filters.value, defaults),
        search: searchDebounced.value || undefined,
        page: page.value !== 1 ? String(page.value) : undefined,
      },
    })
  }, { deep: true })

  const params = computed(() => ({
    pageNumber: page.value,
    pageSize,
    search: searchDebounced.value || undefined,
    ...clean(filters.value, defaults),
  }))

  return { page, search, filters, params }
}

function pick(q: Record<string, unknown>, keys: string[]) {
  return Object.fromEntries(keys.filter((k) => typeof q[k] === 'string').map((k) => [k, q[k] as string]))
}
function clean<F extends Record<string, string | undefined>>(f: F, d: F) {
  return Object.fromEntries(Object.entries(f).filter(([k, v]) => v && v !== d[k])) as Partial<F>
}

// view
const { page, search, filters, params } = useListQueryState({ status: 'all' })
const { data, isLoading } = useUsuariosQuery(params)
```

`router.replace` (não `push`) para não poluir o histórico a cada tecla.

Referência: [Vue Router — Programmatic Navigation](https://router.vuejs.org/guide/essentials/navigation.html)
