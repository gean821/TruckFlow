---
title: Estado derivado é computed, não watch + ref
impact: HIGH
impactDescription: uma fonte de verdade, sem dessincronização
tags: reactivity, computed, watch, derived-state
---

## Estado derivado é computed, não watch + ref

**Impacto: HIGH (uma fonte de verdade, sem dessincronização)**

Se um valor pode ser calculado a partir de outros, ele é `computed`. `watch` serve para **efeitos colaterais** (sincronizar URL, chamar API imperativa, foco) — não para manter cópias.

**Incorreto:**

```ts
const totalPages = ref(1)
watch(data, (v) => {
  totalPages.value = Math.ceil((v?.totalCount ?? 0) / pageSize.value)
})
```

**Correto:**

```ts
const totalPages = computed(() => data.value?.totalPages ?? 1) // o backend já calcula
```

**Uso legítimo de watch (efeito colateral — `ManageUserView.vue`):**

```ts
watch([searchDebounced, filtroStatus, page], () => {
  router.replace({ query: { search: searchDebounced.value || undefined, page: page.value !== 1 ? String(page.value) : undefined } })
})
```

`computed` deve ser puro: sem chamada HTTP, sem mutação de outro estado.

Referência: [Vue — Computed Properties](https://vuejs.org/guide/essentials/computed.html) · [Vue — Watchers](https://vuejs.org/guide/essentials/watchers.html)
