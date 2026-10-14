---
title: v-memo em listas grandes, v-once em conteúdo estático
impact: MEDIUM
impactDescription: pula re-render de linhas que não mudaram
tags: rendering, v-memo, v-once, performance
---

## v-memo em listas grandes, v-once em conteúdo estático

**Impacto: MEDIUM (pula re-render de linhas que não mudaram)**

Em listas com centenas de itens onde um estado global muda (ex.: id selecionado, filtro de destaque), todas as linhas re-renderizam. `v-memo` com as dependências da linha pula as que não mudaram. **Meça antes** — com listas paginadas de 12-50 itens, normalmente não compensa.

**Incorreto (todas as linhas re-renderizam ao trocar a seleção):**

```vue
<div v-for="a in agendamentos" :key="a.id" :class="{ selecionado: a.id === selecionadoId }">
  <AgendamentoCard :agendamento="a" />
</div>
```

**Correto:**

```vue
<div
  v-for="a in agendamentos"
  :key="a.id"
  v-memo="[a.id === selecionadoId, a.status, a.updatedAt]"
  :class="{ selecionado: a.id === selecionadoId }"
>
  <AgendamentoCard :agendamento="a" />
</div>
```

`v-once` para blocos que nunca mudam após o mount (termos LGPD, texto institucional de `SaibaMais.vue`).

Se esquecer uma dependência no `v-memo`, a linha fica desatualizada — é uma otimização com risco de bug; documente o porquê.

Referência: [Vue — v-memo](https://vuejs.org/api/built-in-directives.html#v-memo)
