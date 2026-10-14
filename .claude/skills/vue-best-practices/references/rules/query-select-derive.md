---
title: Derive dados com select, não com cópia em ref
impact: MEDIUM
impactDescription: dado derivado sempre sincronizado com o cache
tags: vue-query, select, derived-state
---

## Derive dados com select, não com cópia em ref

**Impacto: MEDIUM (dado derivado sempre sincronizado com o cache)**

Copiar `data` para um `ref` local via `watch` cria duas fontes de verdade: quando a query refetcha, a cópia fica para trás. Para transformar (opções de `v-select`, séries de gráfico), use `select` na query ou `computed` sobre `data`.

**Incorreto:**

```ts
const { data } = useLocaisDescargaQuery()
const opcoes = ref<{ title: string; value: string }[]>([])
watch(data, (v) => {
  opcoes.value = (v ?? []).map((l) => ({ title: l.nome, value: l.id }))
})
```

**Correto (select — o componente só re-renderiza se o resultado mudar):**

```ts
export function useLocaisDescargaOptionsQuery() {
  return useQuery({
    queryKey: [localDescargaQueryKey, 'options'],
    queryFn: () => service.getAll(),
    select: (locais) => locais.filter((l) => l.ativo).map((l) => ({ title: l.nome, value: l.id })),
    staleTime: 5 * 60 * 1000,
  })
}
```

**Correto (computed, quando a derivação depende de estado da view):**

```ts
const opcoes = computed(() => (data.value ?? []).map((l) => ({ title: l.nome, value: l.id })))
```

Referência: [React Query Data Transformations — TkDodo](https://tkdodo.eu/blog/react-query-data-transformations)
