---
title: Cleanup em watchers e efeitos (onWatcherCleanup / onScopeDispose)
impact: MEDIUM-HIGH
impactDescription: sem timers, listeners e conexões SSE vazando
tags: reactivity, watch, cleanup, memory-leak, sse
---

## Cleanup em watchers e efeitos (onWatcherCleanup / onScopeDispose)

**Impacto: MEDIUM-HIGH (sem timers, listeners e conexões SSE vazando)**

Todo efeito que cria recurso (timer, listener, `AbortController`, conexão `fetchEventSource`) precisa de cleanup. O debounce inline das views usa `let timeout` no escopo do módulo e nunca limpa no unmount.

**Incorreto (debounce inline — `ManageUserView.vue`, `VisualizarAgendamentoView.vue`):**

```ts
let timeout: ReturnType<typeof setTimeout>
watch(search, (val) => {
  clearTimeout(timeout)
  timeout = setTimeout(() => { searchDebounced.value = val ?? '' }, 400)
}) // timer ainda pode disparar após sair da tela
```

**Correto (Vue 3.5 `onWatcherCleanup`):**

```ts
import { onWatcherCleanup, watch } from 'vue'

watch(search, (val) => {
  const t = setTimeout(() => { searchDebounced.value = val ?? '' }, 350)
  onWatcherCleanup(() => clearTimeout(t)) // roda antes da próxima execução E no unmount
})
```

**Correto (conexão de longa duração — padrão para `useRealtimeNotifications`):**

```ts
watch(() => auth.token, (token) => {
  if (!token) return
  const ctrl = new AbortController()
  fetchEventSource(url, { signal: ctrl.signal, headers: { Authorization: `Bearer ${token}` } /* ... */ })
  onWatcherCleanup(() => ctrl.abort()) // token renovado → reconecta com o novo
}, { immediate: true })
```

Note que o watcher acima reage a **qualquer** troca de token — resolve o bug conhecido do SSE seguir com o token antigo após refresh.

Melhor ainda: extraia `useDebouncedRef` (ver `composable-debounced-ref`).

Referência: [Vue — Side Effect Cleanup](https://vuejs.org/guide/essentials/watchers.html#side-effect-cleanup)
