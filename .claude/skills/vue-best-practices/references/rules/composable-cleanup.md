---
title: Composables limpam o que criam (onScopeDispose)
impact: MEDIUM
impactDescription: sem listeners de window/document vazando entre telas
tags: composable, cleanup, event-listeners, memory-leak
---

## Composables limpam o que criam (onScopeDispose)

**Impacto: MEDIUM (sem listeners de window/document vazando entre telas)**

Composables que registram listeners globais (`resize`, `keydown` para atalho da portaria, `visibilitychange`), intervals ou conexões precisam remover no fim do escopo. `onScopeDispose` funciona tanto dentro de componente quanto de `effectScope` (stores).

**Incorreto:**

```ts
export function useAtalhoCheckIn(cb: () => void) {
  window.addEventListener('keydown', (e) => { if (e.key === 'F2') cb() })
}
```

**Correto:**

```ts
import { onScopeDispose } from 'vue'

export function useAtalhoCheckIn(cb: () => void) {
  const handler = (e: KeyboardEvent) => {
    if (e.key === 'F2') { e.preventDefault(); cb() }
  }
  window.addEventListener('keydown', handler)
  onScopeDispose(() => window.removeEventListener('keydown', handler))
}
```

VueUse (`useEventListener`, `useIntervalFn`) já faz o cleanup — mesma lógica.

Referência: [Vue — onScopeDispose](https://vuejs.org/api/reactivity-advanced.html#onscopedispose) · [VueUse — useEventListener](https://vueuse.org/core/useEventListener/)
