---
name: vue-best-practices
description: Guia de boas práticas e performance para o painel admin TruckFlowApp (Vue 3.5 + TanStack Vue Query 5 + Vuetify 3 + Vite 7, SPA). 39 regras priorizadas por impacto — query keys reativas, invalidação, defaults do QueryClient, waterfalls, bundle (Vuetify/ApexCharts), reatividade, renderização, contratos de componente e composables. Use ao escrever, revisar ou refatorar código Vue/TS em C:\ESTUDO\TruckFlowApp, ao investigar tela lenta/desatualizada, ou ao revisar PR do front.
version: 1.0.0
author: TruckFlow (adaptado de Vercel Engineering — react-best-practices)
license: MIT
tags: [Vue, Vue Query, TanStack, Vuetify, Vite, Pinia, Performance, Best Practices, Bundle Size, Reactivity]
dependencies: []
---

# Vue Best Practices — TruckFlowApp

Guia de performance e corretude para o admin web do TruckFlow, com 39 regras organizadas por impacto. Cada regra cita o **padrão de referência real do repo** e marca como "não copie" os trechos que violam a regra hoje.

> SPA pura (Vercel, sem SSR). As regras de Server Components, hidratação e cache de servidor do guia React original foram descartadas — não se aplicam.

## Quando usar

**Use quando:**
- Criar ou alterar `src/queries/*`, `src/hooks/*`, `src/services/*`, `src/views/*`, `src/components/*`
- Revisar PR do front (rode também `senior-frontend/scripts/pattern_auditor.py`)
- Tela mostra dado velho depois de salvar → seção 1 (query)
- Tela lenta para abrir / login pesado → seções 2 e 3 (async, bundle)
- Filtro/busca "não reage" → `query-reactive-keys`, `reactivity-destructuring`
- Componente novo com props/emits/v-model → seção 6

**Áreas cobertas:**
- **Estado de servidor com Vue Query** (CRITICAL)
- **Eliminando waterfalls** (CRITICAL)
- **Tamanho de bundle** (CRITICAL)
- **Reatividade** (HIGH)
- **Renderização** (MEDIUM)
- **API de componentes** (MEDIUM)
- **Composables** (MEDIUM)
- **JavaScript** (LOW-MEDIUM)

## Regras de ouro do projeto (antes de qualquer regra)

1. **HTTP só via hooks/queries.** View/componente nunca importa `XService` nem `http` direto. Pinia nunca guarda dado de servidor.
2. **Listagem de tela é paginada server-side** (`<X>ListQueryDto` → `PaginatedResponse<T>`), com busca debounced (~350ms) e filtros/página em `route.query`.
3. **`<script setup lang="ts">`** sempre; props/emits tipados; zero `any` em código novo.
4. **Edits cirúrgicos**: ao tocar um arquivo legado, corrija o que a tarefa pede — não refatore o arquivo inteiro para o padrão novo sem pedido.

## Referência rápida — prioridades críticas

1. **Query key reativa** — passe o `ref`/`computed` na key, `unref` no `queryFn` (`src/queries/usuario.queries.ts`)
2. **Invalide lista + detalhe** — uma key factory por entidade; mutation invalida `xKeys.all`
3. **Nada de fetch em `onMounted`** — tudo é `useQuery` (com `refetchInterval` se precisar polling)
4. **QueryClient com defaults** — `retry` sem 4xx, `mutations.retry: false`, `QueryCache.onError`
5. **Paralelize** — `Promise.all` para chamadas independentes; não encadeie queries sem necessidade
6. **Bundle** — Vuetify via `vite-plugin-vuetify`, ApexCharts e modais raros com `defineAsyncComponent`

## Padrões comuns

**Query paginada reativa (padrão de referência):**

```ts
// src/queries/usuario.queries.ts
export function useUsuariosQuery(params: MaybeRef<UsuarioListQueryDto>) {
  return useQuery({
    queryKey: [usuarioQueryKey, params],
    queryFn: ({ signal }) => service.getPaged(unref(params), signal),
    placeholderData: keepPreviousData,
  })
}
```

**Detalhe com enabled reativo:**

```ts
export function useUsuarioByIdQuery(id: MaybeRef<string | undefined | null>) {
  return useQuery({
    queryKey: [usuarioQueryKey, id],
    queryFn: () => service.getById(unref(id)!),
    enabled: () => !!unref(id),
  })
}
```

**Mutation no hook, com invalidação e erro do backend:**

```ts
const createMutation = useMutation({
  mutationFn: (payload: UsuarioCreateDto) => service.create(payload),
  onSuccess: async () => {
    await queryClient.invalidateQueries({ queryKey: [usuarioQueryKey] })
    toast.notify('Usuário cadastrado!', 'success')
  },
  onError: (error) => toast.notify(getErrorMessage(error, 'Erro ao cadastrar usuário.'), 'error'),
})
```

**Store sem perder reatividade:**

```ts
const { show, message } = storeToRefs(useToastStore())
```

**Componente pesado sob demanda:**

```ts
const ApexChart = defineAsyncComponent(() => import('vue3-apexcharts'))
```

## Usando as regras

- **`references/vue-performance-guidelines.md`** — guia completo compilado com todas as regras, exemplos e impacto (gerado a partir de `references/rules/`).
- **`references/rules/<prefixo>-<nome>.md`** — uma regra por arquivo (formato em `_template.md`, seções em `_sections.md`).

Para adicionar regra: copie `_template.md`, use o prefixo da seção, cite um arquivo real do repo, e recompile o guia (rode `python references/compile_guidelines.py`).

## Fontes

- [Vue.js — Guia oficial](https://vuejs.org/guide/) e [Performance](https://vuejs.org/guide/best-practices/performance.html) (Evan You e core team)
- [TanStack Query — Vue](https://tanstack.com/query/latest/docs/framework/vue/overview)
- [TkDodo's blog](https://tkdodo.eu/blog/practical-react-query) — Dominik Dorfmeister, mantenedor do TanStack Query: *Practical React Query*, *Effective Query Keys*, *Mastering Mutations*, *Error Handling* (conceitos valem 1:1 para Vue Query)
- [VueUse](https://vueuse.org/) — Anthony Fu: composables de referência (`refDebounced`, `useEventListener`)
- [Michael Thiessen](https://michaelnthiessen.com/) — padrões de composables e componentes Vue
- [Vuetify 3](https://vuetifyjs.com/) — treeshaking, temas, data tables virtuais
- [Vue Router 4](https://router.vuejs.org/) e [Vite](https://vite.dev/guide/)
