---
title: Exponha estado read-only de composables e stores
impact: LOW-MEDIUM
impactDescription: só a action certa muda o estado; bugs de mutação viram erro de tipo
tags: reactivity, readonly, composables, pinia
---

## Exponha estado read-only de composables e stores

**Impacto: LOW-MEDIUM (só a action certa muda o estado; bugs de mutação viram erro de tipo)**

Composables e stores de setup que devolvem `ref` mutável permitem que qualquer componente faça `show.value = false` por fora, contornando a action. Exponha `readonly(...)` e mutação só por função.

**Incorreto:**

```ts
export function useComunicacaoDialog() {
  const aberto = ref(false)
  const agendamentoId = ref<string | null>(null)
  return { aberto, agendamentoId } // qualquer um altera
}
```

**Correto:**

```ts
export function useComunicacaoDialog() {
  const aberto = ref(false)
  const agendamentoId = ref<string | null>(null)
  function abrir(id: string) { agendamentoId.value = id; aberto.value = true }
  function fechar() { aberto.value = false; agendamentoId.value = null }
  return { aberto: readonly(aberto), agendamentoId: readonly(agendamentoId), abrir, fechar }
}
```

Para estado que precisa de `v-model` (dialog), exponha um `computed` get/set que chama a action no set.

Referência: [Vue — readonly()](https://vuejs.org/api/reactivity-core.html#readonly)
