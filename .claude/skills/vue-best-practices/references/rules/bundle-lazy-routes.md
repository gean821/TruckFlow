---
title: Toda rota autenticada é lazy (import dinâmico)
impact: CRITICAL
impactDescription: login carrega só o necessário; cada tela vira um chunk
tags: bundle, code-splitting, vue-router, lazy-loading
---

## Toda rota autenticada é lazy (import dinâmico)

**Impacto: CRITICAL (login carrega só o necessário; cada tela vira um chunk)**

O router já usa `() => import(...)` para quase tudo — mantenha. Só `LoginView`/`HomePage` podem ser eager. Nunca importe uma view estaticamente em outro lugar (isso puxa o chunk para o bundle principal).

**Incorreto:**

```ts
import DashboardView from '@/views/DashboardView.vue'
const routes = [{ path: '/dashboard', component: DashboardView }]
```

**Correto (`src/router/index.ts`):**

```ts
const DashboardView = () => import('@/views/DashboardView.vue')
const routes = [
  { path: '/dashboard', name: 'dashboard', component: DashboardView,
    meta: { requiresAuth: true, roles: RoleGroups.CanViewSchedule } },
]
```

Também não roteie para arquivos de `components/` (hoje `/nova-grade` → `components/cards/GradeCard.vue`, `/novo-recebimento` → `components/Forms/RecebimentoForm.vue`): páginas vivem em `views/`.

Referência: [Vue Router — Lazy Loading Routes](https://router.vuejs.org/guide/advanced/lazy-loading.html)
