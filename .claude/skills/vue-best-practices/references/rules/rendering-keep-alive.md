---
title: KeepAlive seletivo para telas de ida-e-volta
impact: LOW-MEDIUM
impactDescription: preserva scroll e estado local ao voltar do detalhe
tags: rendering, keep-alive, router
---

## KeepAlive seletivo para telas de ida-e-volta

**Impacto: LOW-MEDIUM (preserva scroll e estado local ao voltar do detalhe)**

Fluxo típico da portaria: lista de agendamentos → `/conferencia/:agendamentoId` → volta. Sem cache, a lista remonta, perde scroll e estado de UI. Filtros/página já sobrevivem via `route.query`; `KeepAlive` cobre o resto (scroll, painéis expandidos).

**Incorreto (KeepAlive em tudo — telas com SSE/timers continuam vivas em background):**

```vue
<RouterView v-slot="{ Component }">
  <KeepAlive><component :is="Component" /></KeepAlive>
</RouterView>
```

**Correto (lista explícita, com limite):**

```vue
<RouterView v-slot="{ Component }">
  <KeepAlive :include="['VisualizarAgendamentoView']" :max="3">
    <component :is="Component" />
  </KeepAlive>
</RouterView>
```

Componentes cacheados recebem `onActivated`/`onDeactivated` em vez de mount/unmount — pause timers em `onDeactivated`. `include` casa com o **nome** do componente (em `<script setup>`, o nome do arquivo ou `defineOptions({ name })`).

Referência: [Vue — KeepAlive](https://vuejs.org/guide/built-ins/keep-alive.html)
