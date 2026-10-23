---
title: keepPreviousData em toda listagem paginada/filtrada
impact: HIGH
impactDescription: elimina o "pisca" de skeleton ao trocar de página ou filtro
tags: vue-query, pagination, placeholder-data, ux
---

## keepPreviousData em toda listagem paginada/filtrada

**Impacto: HIGH (elimina o "pisca" de skeleton ao trocar de página ou filtro)**

Toda listagem de tela é paginada server-side (`<Entidade>ListQueryDto` → `PaginatedResponse<T>`). Sem `placeholderData: keepPreviousData`, cada troca de página vira `data = undefined` → skeleton → dados, e a `v-pagination` some e volta.

**Incorreto:**

```ts
return useQuery({
  queryKey: [gradeQueryKey, params],
  queryFn: () => service.getPaged(unref(params)),
})
```

**Correto:**

```ts
import { keepPreviousData, useQuery } from '@tanstack/vue-query'

return useQuery({
  queryKey: [gradeQueryKey, params],
  queryFn: () => service.getPaged(unref(params)),
  placeholderData: keepPreviousData,
})
```

Na view, skeleton só quando **não há nada** (`v-if="isLoading && !data"`) e um indicador leve (`<v-progress-linear indeterminate v-if="isFetching" />`) durante a troca.

Referência: [Paginated Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/paginated-queries)
