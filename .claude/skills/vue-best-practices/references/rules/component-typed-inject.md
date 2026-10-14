---
title: provide/inject com InjectionKey tipado
impact: LOW-MEDIUM
impactDescription: inject sem any e sem string mágica
tags: component, provide-inject, typescript
---

## provide/inject com InjectionKey tipado

**Impacto: LOW-MEDIUM (inject sem any e sem string mágica)**

Para estado compartilhado numa subárvore (ex.: tela de conferência com vários painéis lendo o mesmo agendamento), `provide/inject` evita prop drilling. Use `InjectionKey<T>` — com string, o `inject` retorna `unknown`.

**Incorreto:**

```ts
provide('agendamento', agendamento)
const agendamento = inject('agendamento') as any
```

**Correto:**

```ts
// src/views/conferencia/conferencia.context.ts
import type { InjectionKey, Ref } from 'vue'
export interface ConferenciaContext {
  agendamento: Readonly<Ref<AgendamentoResponse | undefined>>
  recarregar: () => Promise<void>
}
export const ConferenciaKey: InjectionKey<ConferenciaContext> = Symbol('Conferencia')

// pai
provide(ConferenciaKey, { agendamento: readonly(agendamento), recarregar })

// filho
const ctx = inject(ConferenciaKey)
if (!ctx) throw new Error('ConferenciaKey não fornecido — use dentro de ConferenciaView')
```

Não use provide/inject para estado global (isso é Pinia, se for UI/sessão; Vue Query, se for servidor).

Referência: [Vue — Typing provide/inject](https://vuejs.org/guide/typescript/composition-api.html#typing-provide-inject)
