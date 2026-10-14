---
title: key estável (id do backend) em todo v-for
impact: HIGH
impactDescription: evita estado de linha trocado e re-render total da lista
tags: rendering, v-for, key
---

## key estável (id do backend) em todo v-for

**Impacto: HIGH (evita estado de linha trocado e re-render total da lista)**

`:key="index"` faz o Vue reaproveitar o DOM pela posição: ao paginar, filtrar ou remover um item, o estado interno (menu aberto, input digitado, loading do botão) "pula" para o item errado. Toda entidade do backend tem `id: string` (Guid) — use.

**Incorreto:**

```vue
<v-card v-for="(u, i) in data?.items" :key="i">
```

**Correto:**

```vue
<v-card v-for="u in data?.items" :key="u.id">
```

Também use `:key` para **resetar** um componente de formulário entre criar/editar, como `ManageUserView` faz: `<UsuarioFormModal :key="editingUser?.id ?? 'new'" ... />` — mais simples e seguro que `watch(initialData)` + `Object.assign`.

Referência: [Vue — Maintaining State with key](https://vuejs.org/guide/essentials/list.html#maintaining-state-with-key)
