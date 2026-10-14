---
title: Tipar slots de componentes genéricos (defineSlots + generic)
impact: LOW-MEDIUM
impactDescription: tabela/lista genérica com autocomplete do item no slot
tags: component, slots, generics, typescript
---

## Tipar slots de componentes genéricos (defineSlots + generic)

**Impacto: LOW-MEDIUM (tabela/lista genérica com autocomplete do item no slot)**

Componentes reutilizáveis de listagem (o `CrudTable.vue` atual, ou um futuro `PagedList.vue`) recebem itens de qualquer entidade. Com `generic` e `defineSlots`, o slot conhece o tipo do item.

**Incorreto:**

```ts
const props = defineProps<{ items: any[]; headers: any[] }>()
```

**Correto:**

```vue
<script setup lang="ts" generic="T extends { id: string }">
const props = defineProps<{
  items: T[]
  loading?: boolean
  totalPages: number
}>()
const page = defineModel<number>('page', { required: true })

defineSlots<{
  item(props: { item: T }): any
  empty(): any
}>()
</script>

<template>
  <v-skeleton-loader v-if="loading && !items.length" type="card@3" />
  <template v-else-if="items.length">
    <template v-for="item in items" :key="item.id">
      <slot name="item" :item="item" />
    </template>
    <v-pagination v-model="page" :length="totalPages" :total-visible="7" />
  </template>
  <slot v-else name="empty" />
</template>
```

Referência: [Vue — Generics](https://vuejs.org/api/sfc-script-setup.html#generics) · [defineSlots](https://vuejs.org/api/sfc-script-setup.html#defineslots)
