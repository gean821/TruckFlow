---
name: vue-router-vite-best-practices
description: Princípios de Vue Router 4 + Vite 7 para o TruckFlowApp (SPA na Vercel) — rotas lazy, RouteMeta tipado (requiresAuth/roles/layout), layouts aninhados com RouterView, guards que retornam valor, página 403, query params como estado, env tipado (ImportMetaEnv/.env.example), alias, manualChunks, vite-plugin-vuetify e análise de bundle. Use ao criar/alterar rotas, guards, layouts, configuração do Vite ou variáveis de ambiente do front.
allowed-tools: Read, Write, Edit, Glob, Grep
---

# Vue Router + Vite — TruckFlowApp

> Princípios para roteamento e build do admin web. Arquivos-chave: `src/router/index.ts`, `src/router/routeMeta.ts`, `src/shared/auth/roles.ts`, `src/main.ts`, `vite.config.ts`, `vercel.json`.

---

## 1. Rotas: lazy por padrão

| Tipo | Import |
|---|---|
| `LoginView`, `HomePage` (públicas de entrada) | eager (ok) |
| **Qualquer rota autenticada** | `() => import('@/views/XView.vue')` |
| Componentes de `components/` | **nunca** são alvo de rota — páginas vivem em `views/` |

Hoje `/nova-grade` aponta para `components/cards/GradeCard.vue` e `/novo-recebimento` para `components/Forms/RecebimentoForm.vue` — **não copie**. Rota nova = `views/<Nome>View.vue`.

---

## 2. RouteMeta tipado (inclusive `layout`)

O `meta.layout: 'public'` existe nas rotas mas não está tipado nem é lido. Tipar tudo que se usa:

```ts
// src/router/routeMeta.ts
import 'vue-router'
import type { Role } from '@/shared/auth/roles'

declare module 'vue-router' {
  interface RouteMeta {
    requiresAuth?: boolean
    roles?: readonly Role[]
    layout?: 'public' | 'app'
    title?: string // usado no document.title e no breadcrumb
  }
}
```

Roles sempre via `RoleGroups` (`CanManageUsers`, `CanCheckIn`, ...) — nunca array literal de strings na rota.

---

## 3. Layouts aninhados em vez de `v-if` no App.vue

Hoje `App.vue` faz `<Navbar v-if="router.currentRoute.value.meta.requiresAuth" />`. Alvo: layout como rota-pai.

```ts
const routes: RouteRecordRaw[] = [
  {
    path: '/',
    component: () => import('@/layouts/PublicLayout.vue'),
    children: [
      { path: '', redirect: '/home' },
      { path: 'home', name: 'home', component: HomePage },
      { path: 'login', name: 'login', component: LoginView },
    ],
  },
  {
    path: '/',
    component: () => import('@/layouts/AppLayout.vue'), // Navbar + <RouterView />
    meta: { requiresAuth: true },
    children: [
      { path: 'usuarios', name: 'usuarios', component: () => import('@/views/ManageUserView.vue'),
        meta: { roles: RoleGroups.CanManageUsers, title: 'Usuários' } },
    ],
  },
  { path: '/403', name: 'forbidden', component: () => import('@/views/ForbiddenView.vue') },
  { path: '/:pathMatch(.*)*', name: 'not-found', component: () => import('@/views/NotFoundView.vue') },
]
```

`meta` é mesclado de pai para filho em `to.meta` — `requiresAuth` no layout vale para todos os filhos.

---

## 4. Guards: retorne valor, não chame `next()`

`next()` é API legada (fácil chamar duas vezes ou esquecer). Vue Router 4 aceita retorno.

```ts
router.beforeEach((to) => {
  const auth = useAuthStore()
  if (!to.meta.requiresAuth) return true
  if (!auth.isAuthenticated) return { name: 'login', query: { redirect: to.fullPath } }
  if (to.meta.roles && !hasRole(auth.user?.role, to.meta.roles)) return { name: 'forbidden' }
  return true
})
```

| ❌ Hoje | ✅ Alvo |
|---|---|
| Papel errado → `/login` (usuário logado acha que caiu a sessão) | Papel errado → `/403` com "Você não tem acesso a esta área" |
| Login sempre vai para `/visualizar` | Respeita `?redirect=` (validando que é path interno: começa com `/` e não com `//`) |
| `next('/login')` | `return { name: 'login' }` |

**O guard é UX, não segurança.** Quem garante acesso é o `[Authorize(Roles = ...)]` no backend. Nunca esconda só no front algo que o backend não protege.

`restoreSession()` é aguardado no `main.ts` antes do `app.use(router)` — mantenha, senão o primeiro guard roda sem token e manda para `/login` num refresh de página.

---

## 5. Query params são estado

| Estado | Onde vive |
|---|---|
| Página, busca, filtros de listagem | `route.query` (via `router.replace`) |
| Id do recurso | `route.params` (`/conferencia/:agendamentoId`) |
| Dialog aberto, item em edição | `ref` local |
| Sessão, toast | Pinia |
| Dados do servidor | Vue Query |

Use `props: true` (ou função) na rota para receber params como props tipadas em vez de ler `useRoute()` dentro do componente:

```ts
{ path: 'conferencia/:agendamentoId', component: () => import('@/views/ConferenciaView.vue'), props: true }
```

Ver composable `useListQueryState` na skill `vue-best-practices` (regra `composable-url-query-state`).

---

## 6. Scroll e título

```ts
const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
  scrollBehavior: (to, _from, saved) => saved ?? (to.hash ? { el: to.hash } : { top: 0 }),
})

router.afterEach((to) => {
  document.title = to.meta.title ? `${to.meta.title} · TruckFlow` : 'TruckFlow'
})
```

`index.html` ainda tem `<title>Vite App</title>` — corrija junto.

---

## 7. Vite: variáveis de ambiente

| Regra | Como |
|---|---|
| Só `VITE_*` chega ao bundle | e **tudo** que chega é público — nunca segredo |
| Tipar | `ImportMetaEnv` em `src/env.d.ts` |
| Documentar | `.env.example` versionado (hoje não existe) |
| Barra final | `VITE_API_URL` termina em `/v1/` — o SSE concatena `notifications/stream` |

```ts
// src/env.d.ts
/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly VITE_API_URL: string
}
interface ImportMeta {
  readonly env: ImportMetaEnv
}
```

```bash
# .env.example
VITE_API_URL=http://localhost:8080/v1/
```

Na Vercel, a variável vai no painel do projeto (Production/Preview). Mudou a var → precisa de rebuild (é embutida em build time).

---

## 8. Vite: build e bundle

```ts
// vite.config.ts
import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import vueDevTools from 'vite-plugin-vue-devtools'

export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true }), vueDevTools()],
  resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vuetify: ['vuetify'],
          charts: ['apexcharts', 'vue3-apexcharts'],
          query: ['@tanstack/vue-query', 'axios'],
        },
      },
    },
  },
})
```

| Ferramenta | Uso |
|---|---|
| `pnpm build` | roda `vue-tsc` + `vite build` (é o que o CI roda) |
| `rollup-plugin-visualizer` | `ANALYZE=1 pnpm build-only` → `stats.html` treemap |
| `senior-frontend/scripts/bundle_analyzer.py` | lista chunks > 250KB em `dist/assets` e detecta `import * as components` |

Gerenciador de pacotes é **pnpm** (`pnpm-lock.yaml`, CI com `--frozen-lockfile`). Não rode `npm install` (gera `package-lock.json` concorrente).

---

## 9. Deploy SPA na Vercel

`vercel.json` reescreve `/(.*)` → `/index.html` (history mode). Se adicionar assets fora de `dist/` ou rotas de API, ajuste o rewrite para não engolir `/assets/*`. Cache: arquivos em `/assets/` têm hash no nome → `Cache-Control: immutable`; `index.html` sem cache.

---

## 10. Anti-padrões

| ❌ Não | ✅ Sim |
|---|---|
| Import estático de view no router | `() => import(...)` |
| Rota apontando para `components/` | Página em `views/` |
| `next()` em guard | Retornar `true` / location |
| Papel errado → `/login` | `/403` |
| `meta.layout` não tipado e não lido | `RouteMeta` augmentation + layout como rota-pai |
| Breadcrumb com `routeMap` hardcoded (com rotas inexistentes) | `meta.title` + `route.matched` |
| `import.meta.env.VITE_API_URL as string` | `ImportMetaEnv` tipado |
| Segredo em `VITE_*` | Segredo fica no backend |
| `npm install` | `pnpm install` |

---

## 11. Estrutura alvo

```
src/
├── layouts/        # PublicLayout.vue, AppLayout.vue (Navbar + RouterView)
├── router/         # index.ts (routes + guards), routeMeta.ts (augmentation)
├── views/          # páginas roteadas (*View.vue) — únicas que o router conhece
├── components/     # nunca roteados
├── composables/    # useDebouncedRef, useListQueryState, useRealtimeNotifications
├── plugins/        # vuetify.ts, vue-query.ts
└── env.d.ts        # ImportMetaEnv
```

> **Lembre:** rota é contrato de navegação, não de segurança. Lazy por padrão, meta tipado, backend autoriza.

Fontes: [Vue Router — Navigation Guards](https://router.vuejs.org/guide/advanced/navigation-guards.html) · [Lazy Loading](https://router.vuejs.org/guide/advanced/lazy-loading.html) · [Typed RouteMeta](https://router.vuejs.org/guide/advanced/meta.html#TypeScript) · [Vite — Env Variables](https://vite.dev/guide/env-and-mode) · [Vite — Build](https://vite.dev/guide/build) · [Vercel — SPA rewrites](https://vercel.com/docs/frameworks/vite)
