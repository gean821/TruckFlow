---
title: Não perca reatividade ao desestruturar (props, reactive, stores)
impact: HIGH
impactDescription: evita valor "congelado" que não atualiza na tela
tags: reactivity, toRefs, storeToRefs, props, pinia
---

## Não perca reatividade ao desestruturar (props, reactive, stores)

**Impacto: HIGH (evita valor "congelado" que não atualiza na tela)**

Desestruturar um `reactive` ou uma store Pinia copia o valor do momento. Props desestruturadas são reativas desde Vue 3.5 **dentro do template e de computed/watch com getter**, mas passar a variável crua para uma função/composable perde a reatividade.

**Incorreto:**

```ts
const auth = useAuthStore()
const { token, user } = auth          // congela; logout não reflete
const { message, show } = useToastStore() // AppToast nunca fecharia
```

**Correto (`components/common/AppToast.vue` faz certo):**

```ts
import { storeToRefs } from 'pinia'
const toast = useToastStore()
const { show, message, type, timeout } = storeToRefs(toast) // estado vira refs
const { notify } = toast                                     // actions podem ser desestruturadas
```

**Props passadas para composable (Vue 3.5):**

```ts
const { agendamentoId } = defineProps<{ agendamentoId: string }>()
useAgendamentoByIdQuery(agendamentoId)          // ❌ string crua, não reage
useAgendamentoByIdQuery(() => agendamentoId)    // ✅ getter (query aceita MaybeRefOrGetter + toValue)
```

Referência: [Vue — Reactive Props Destructure](https://vuejs.org/guide/components/props.html#reactive-props-destructure) · [Pinia — storeToRefs](https://pinia.vuejs.org/core-concepts/#Destructuring-from-a-Store)
