---
title: Query keys e parâmetros reativos (MaybeRef + unref)
impact: CRITICAL
impactDescription: lista passa a reagir a filtro/paginação sem refetch manual
tags: vue-query, query-key, reactivity, maybe-ref
---

## Query keys e parâmetros reativos (MaybeRef + unref)

**Impacto: CRITICAL (lista passa a reagir a filtro/paginação sem refetch manual)**

No Vue Query, a `queryKey` é observada profundamente: se você passar um `ref`/`computed` **dentro** da key, a query refaz o fetch sozinha quando o valor muda. Se você passar `.value`, a key é avaliada uma única vez na criação e congela.

**Incorreto (key avaliada uma vez — `src/queries/empresa.queries.ts`, não copie):**

```ts
export function useEmpresaQuery() {
  const empresaId = computed(() => auth.empresaId)
  return useQuery({
    queryKey: [empresaQueryKey, empresaId.value], // congela no valor inicial
    queryFn: () => service.getById(empresaId.value!),
  })
}
```

**Correto (padrão de referência — `src/queries/usuario.queries.ts`):**

```ts
export function useUsuariosQuery(params: MaybeRef<UsuarioListQueryDto>) {
  return useQuery({
    queryKey: [usuarioQueryKey, params],          // ref dentro da key
    queryFn: async () => await service.getPaged(unref(params)),
    placeholderData: keepPreviousData,
  })
}

// na view
const params = computed<UsuarioListQueryDto>(() => ({
  pageNumber: page.value,
  pageSize: pageSize.value,
  search: searchDebounced.value || undefined,
}))
const { data, isLoading } = useUsuariosQuery(params)
```

Regra prática: toda função `useXxxQuery` recebe `MaybeRef<T>` (ou `MaybeRefOrGetter<T>` + `toValue`) e nunca `T` cru.

Referência: [TanStack Vue Query — Reactivity](https://tanstack.com/query/latest/docs/framework/vue/reactivity)
