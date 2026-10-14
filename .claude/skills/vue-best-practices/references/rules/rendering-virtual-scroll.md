---
title: Virtualize listas longas não paginadas
impact: MEDIUM
impactDescription: DOM constante independente do tamanho da lista
tags: rendering, virtual-scroll, vuetify, v-data-table-virtual
---

## Virtualize listas longas não paginadas

**Impacto: MEDIUM (DOM constante independente do tamanho da lista)**

A regra do projeto é **paginar server-side** toda listagem de tela. Mas há listas que legitimamente vêm inteiras: itens de uma NF-e, timeline de auditoria de uma entidade, seleção de produtos (`ProdutoSelectModal`). Acima de ~200 linhas, virtualize.

**Incorreto (500 itens de NF-e renderizados de uma vez):**

```vue
<v-data-table :items="itensNota" :headers="headers" :items-per-page="-1" />
```

**Correto:**

```vue
<v-data-table-virtual :items="itensNota" :headers="headers" height="480" fixed-header />
```

**Lista simples:**

```vue
<v-virtual-scroll :items="produtos" height="400" item-height="48">
  <template #default="{ item }">
    <v-list-item :key="item.id" :title="item.nome" :subtitle="item.codigo" />
  </template>
</v-virtual-scroll>
```

Virtualização **não** substitui paginação server-side: se a lista pode crescer sem limite (agendamentos, usuários), ela é paginada.

Referência: [Vuetify — Virtual tables](https://vuetifyjs.com/en/components/data-tables/virtual-tables/) · [Vuetify — v-virtual-scroll](https://vuetifyjs.com/en/components/virtual-scroller/)
