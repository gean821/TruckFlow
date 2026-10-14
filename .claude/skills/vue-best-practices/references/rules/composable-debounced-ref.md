---
title: Extraia useDebouncedRef (debounce de busca único no projeto)
impact: MEDIUM
impactDescription: um debounce consistente (350ms) com cleanup, em vez de 3 cópias
tags: composable, debounce, search, pagination
---

## Extraia useDebouncedRef (debounce de busca único no projeto)

**Impacto: MEDIUM (um debounce consistente com cleanup, em vez de 3 cópias)**

O debounce de busca está copiado em `ManageUserView` (400ms), `VisualizarAgendamentoView` (350ms) e `RecebimentosOrfaosView` (350ms), cada um com `let timeout` sem cleanup. A convenção do projeto é **~350ms, server-side**.

**Incorreto (copiado em cada view):**

```ts
let timeout: ReturnType<typeof setTimeout>
watch(search, (val) => {
  clearTimeout(timeout)
  timeout = setTimeout(() => { searchDebounced.value = val ?? "" }, 400)
})
```

**Correto:**

```ts
// src/composables/useDebouncedRef.ts
import { onWatcherCleanup, ref, watch, type Ref } from 'vue'

export function useDebouncedRef<T>(source: Ref<T>, delay = 350): Readonly<Ref<T>> {
  const debounced = ref(source.value) as Ref<T>
  watch(source, (val) => {
    const t = setTimeout(() => { debounced.value = val }, delay)
    onWatcherCleanup(() => clearTimeout(t))
  })
  return debounced
}

// view
const search = ref(route.query.search?.toString() ?? '')
const searchDebounced = useDebouncedRef(search)
```

Alternativa madura: `refDebounced` do VueUse (Anthony Fu). Adicionar a dependência é decisão do time — o composable acima tem 10 linhas e zero deps.

Referência: [VueUse — refDebounced](https://vueuse.org/shared/refDebounced/) · [Vue — Composables](https://vuejs.org/guide/reusability/composables.html)
