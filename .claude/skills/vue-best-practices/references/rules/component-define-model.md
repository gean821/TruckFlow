---
title: defineModel para v-model de dialogs e inputs (Vue 3.4+)
impact: MEDIUM
impactDescription: remove o boilerplate props+emit+computed get/set
tags: component, defineModel, v-model, dialogs
---

## defineModel para v-model de dialogs e inputs (Vue 3.4+)

**Impacto: MEDIUM (remove o boilerplate props+emit+computed get/set)**

O repo usa o padrão antigo `modelValue` + `update:modelValue` + `computed` get/set em todos os dialogs. Com Vue 3.5, `defineModel` faz o mesmo em uma linha e já é tipado. Componentes novos usam `defineModel`; os antigos migram quando forem tocados (edits cirúrgicos — não refatore em massa).

**Antigo (funciona, mas é boilerplate — `ConfirmDialog.vue`):**

```ts
const props = defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()
const open = computed({
  get: () => props.modelValue,
  set: (v: boolean) => emit('update:modelValue', v),
})
```

**Novo:**

```ts
const open = defineModel<boolean>({ required: true })
// múltiplos models:
const search = defineModel<string>('search', { default: '' })
```

```vue
<v-dialog v-model="open" max-width="480" persistent>
```

Form modals que hoje usam `open` + `@close` (`UsuarioFormModal`) podem continuar assim — o padrão é válido; só não misture os dois no mesmo componente.

Referência: [Vue — Component v-model](https://vuejs.org/guide/components/v-model.html)
