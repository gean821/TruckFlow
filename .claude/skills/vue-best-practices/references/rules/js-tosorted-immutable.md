---
title: toSorted()/toReversed() em vez de sort() sobre dado reativo
impact: MEDIUM
impactDescription: não muta o cache do Vue Query nem dispara loop reativo
tags: javascript, immutability, sort, vue-query
---

## toSorted()/toReversed() em vez de sort() sobre dado reativo

**Impacto: MEDIUM (não muta o cache do Vue Query nem dispara loop reativo)**

`.sort()` muta o array original. Dentro de um `computed` sobre `data.value` do Vue Query, isso **altera o cache** compartilhado com outras telas e pode causar loop de reatividade.

**Incorreto:**

```ts
const ordenados = computed(() =>
  (data.value?.items ?? []).sort((a, b) => a.dataAgendamento.localeCompare(b.dataAgendamento)),
)
```

**Correto (ES2023 — suportado pelo target do Vite 7):**

```ts
const ordenados = computed(() =>
  (data.value?.items ?? []).toSorted((a, b) => a.dataAgendamento.localeCompare(b.dataAgendamento)),
)
```

Se a ordenação é da listagem paginada, ela pertence ao **backend** (`OrderBy` no QueryDto) — ordenar client-side só a página atual é bug de UX.

Referência: [MDN — Array.prototype.toSorted](https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/Array/toSorted)
