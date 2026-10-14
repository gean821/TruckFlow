---
title: shallowRef/markRaw para listas grandes e objetos de terceiros
impact: MEDIUM
impactDescription: evita proxy profundo em milhares de objetos
tags: reactivity, shallowRef, markRaw, performance
---

## shallowRef/markRaw para listas grandes e objetos de terceiros

**Impacto: MEDIUM (evita proxy profundo em milhares de objetos)**

`ref`/`reactive` tornam tudo profundamente reativo. Para listas grandes que você substitui inteiras (export CSV com `pageSize: 1000`, dados de gráfico) ou instâncias de bibliotecas (chart, EventSource, AbortController), a reatividade profunda é custo sem benefício.

Dados vindos do Vue Query já são tratados pela lib — esta regra vale para estado **local**.

**Incorreto:**

```ts
const linhasExport = ref<AgendamentoResponse[]>([]) // 1000 objetos com proxy profundo
const chartInstance = ref<ApexCharts | null>(null)
```

**Correto:**

```ts
const linhasExport = shallowRef<AgendamentoResponse[]>([])
linhasExport.value = resultado.items // substituir inteiro dispara a reatividade

const chartInstance = shallowRef<ApexCharts | null>(null)
```

Com `shallowRef`, `linhasExport.value.push(x)` **não** dispara update — substitua o array (`linhasExport.value = [...linhasExport.value, x]`) ou chame `triggerRef`.

Referência: [Vue — Reducing Reactivity Overhead for Large Immutable Structures](https://vuejs.org/guide/best-practices/performance.html#reduce-reactivity-overhead-for-large-immutable-structures)
