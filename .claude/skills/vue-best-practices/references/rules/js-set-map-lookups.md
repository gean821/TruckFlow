---
title: Set/Map para lookups repetidos
impact: LOW-MEDIUM
impactDescription: O(n) para O(1) em filtros client-side
tags: javascript, set, map, data-structures
---

## Set/Map para lookups repetidos

**Impacto: LOW-MEDIUM (O(n) para O(1) em filtros client-side)**

Em filtros client-side de listas pequenas (fornecedor ↔ produtos vinculados, seleção em `ProdutoSelectModal`), `array.includes`/`find` dentro de outro loop vira O(n·m).

**Incorreto:**

```ts
const disponiveis = computed(() =>
  produtos.value.filter((p) => !fornecedor.value?.produtos?.some((fp) => fp.id === p.id)),
)
```

**Correto:**

```ts
const vinculados = computed(() => new Set(fornecedor.value?.produtos?.map((p) => p.id)))
const disponiveis = computed(() => produtos.value.filter((p) => !vinculados.value.has(p.id)))

// lookup por id para montar labels
const produtoPorId = computed(() => new Map(produtos.value.map((p) => [p.id, p])))
const nome = produtoPorId.value.get(item.produtoId)?.nome ?? '—'
```

Referência: [MDN — Set](https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/Set)
