# Vue Best Practices — Guia completo (TruckFlowApp)

> Compilado automaticamente a partir de `rules/*.md` por `compile_guidelines.py`. Não edite aqui: edite a regra e recompile.

> Stack: Vue 3.5 · TanStack Vue Query 5 · Vuetify 3 · Vite 7 · Pinia 3 · SPA (sem SSR).

## Sumário

- **1. Estado de servidor com TanStack Vue Query** (CRITICAL)
  - 1.1 Mutations invalidam lista E detalhe (e entidades afetadas) — `CRITICAL`
  - 1.2 Nada de fetch manual em onMounted/setInterval — use query — `CRITICAL`
  - 1.3 Query keys e parâmetros reativos (MaybeRef + unref) — `CRITICAL`
  - 1.4 enabled reativo (getter), nunca booleano estático — `HIGH`
  - 1.5 QueryClient com defaults globais e QueryCache.onError — `HIGH`
  - 1.6 keepPreviousData em toda listagem paginada/filtrada — `HIGH`
  - 1.7 Query key factory por entidade — `HIGH`
  - 1.8 Mensagem de erro do backend no toast (com fallback) — `HIGH`
  - 1.9 staleTime por tipo de recurso — `MEDIUM-HIGH`
  - 1.10 Update otimista com rollback para toggles de status — `MEDIUM`
  - 1.11 Prefetch do detalhe na intenção (hover/foco) — `MEDIUM`
  - 1.12 Derive dados com select, não com cópia em ref — `MEDIUM`
- **2. Eliminando waterfalls** (CRITICAL)
  - 2.1 Evite waterfall de queries dependentes desnecessárias — `CRITICAL`
  - 2.2 Promise.all para operações independentes — `CRITICAL`
  - 2.3 Repasse o AbortSignal do queryFn para o axios — `MEDIUM-HIGH`
- **3. Tamanho de bundle** (CRITICAL)
  - 3.1 Toda rota autenticada é lazy (import dinâmico) — `CRITICAL`
  - 3.2 Vuetify com tree-shaking (vite-plugin-vuetify), não import * as components — `CRITICAL`
  - 3.3 defineAsyncComponent para dialogs e gráficos pesados — `HIGH`
  - 3.4 Ícones MDI — font inteira vs @mdi/js por ícone — `MEDIUM`
- **4. Reatividade** (HIGH)
  - 4.1 Estado derivado é computed, não watch + ref — `HIGH`
  - 4.2 Não perca reatividade ao desestruturar (props, reactive, stores) — `HIGH`
  - 4.3 Cleanup em watchers e efeitos (onWatcherCleanup / onScopeDispose) — `MEDIUM-HIGH`
  - 4.4 shallowRef/markRaw para listas grandes e objetos de terceiros — `MEDIUM`
  - 4.5 Exponha estado read-only de composables e stores — `LOW-MEDIUM`
- **5. Renderização** (MEDIUM)
  - 5.1 key estável (id do backend) em todo v-for — `HIGH`
  - 5.2 v-memo em listas grandes, v-once em conteúdo estático — `MEDIUM`
  - 5.3 v-if para o raro e caro, v-show para o que alterna muito — `MEDIUM`
  - 5.4 Virtualize listas longas não paginadas — `MEDIUM`
  - 5.5 KeepAlive seletivo para telas de ida-e-volta — `LOW-MEDIUM`
- **6. API de componentes** (MEDIUM)
  - 6.1 defineProps e defineEmits tipados (tupla), nunca array de strings nem any — `HIGH`
  - 6.2 defineModel para v-model de dialogs e inputs (Vue 3.4+) — `MEDIUM`
  - 6.3 provide/inject com InjectionKey tipado — `LOW-MEDIUM`
  - 6.4 Tipar slots de componentes genéricos (defineSlots + generic) — `LOW-MEDIUM`
- **7. Composables** (MEDIUM)
  - 7.1 useUrlQueryState — filtros e página sincronizados com route.query — `MEDIUM-HIGH`
  - 7.2 Composables limpam o que criam (onScopeDispose) — `MEDIUM`
  - 7.3 Extraia useDebouncedRef (debounce de busca único no projeto) — `MEDIUM`
- **8. Performance de JavaScript** (LOW-MEDIUM)
  - 8.1 toSorted()/toReversed() em vez de sort() sobre dado reativo — `MEDIUM`
  - 8.2 Early return em handlers e computeds — `LOW-MEDIUM`
  - 8.3 Set/Map para lookups repetidos — `LOW-MEDIUM`

---

# 1. Estado de servidor com TanStack Vue Query

**Impacto da seção:** CRITICAL

## 1.1 Mutations invalidam lista E detalhe (e entidades afetadas)

**Impacto: CRITICAL (evita detalhe/dashboard desatualizado após editar ou fazer check-in)**

Toda mutation invalida tudo que ela muda. Check-in de agendamento muda o agendamento, o dashboard e a ocupação da grade. `usePlanejamento` faz isso certo (4 keys); `useAgendamento`/`useGrade` invalidam só a lista.

**Incorreto (`src/hooks/useGrade.ts`, não copie):**

```ts
onSuccess: () => {
  queryClient.invalidateQueries({ queryKey: ['grades'] }) // ['grade', id] fica velho
}
```

**Incorreto 2 (refetch manual por cima — `VisualizarAgendamentoView.vue`, não copie):**

```ts
await checkIn(id)
await refetch() // redundante; a mutation já deveria invalidar
```

**Correto:**

```ts
const invalidate = () =>
  Promise.all([
    queryClient.invalidateQueries({ queryKey: agendamentoKeys.all }),
    queryClient.invalidateQueries({ queryKey: ['dashboard'] }),
    queryClient.invalidateQueries({ queryKey: gradeKeys.all }),
  ])

const checkInMutation = useMutation({
  mutationFn: (id: string) => service.checkIn(id),
  onSuccess: () => invalidate(), // retornar a Promise mantém isPending até o refetch
})
```

Retornar a Promise de `invalidateQueries` no `onSuccess` mantém o botão em loading até os dados novos chegarem — sem "clique, sucesso, dado antigo por 300ms".

Referência: [Invalidations from Mutations](https://tanstack.com/query/latest/docs/framework/vue/guides/invalidations-from-mutations) · [Mastering Mutations — TkDodo](https://tkdodo.eu/blog/mastering-mutations-in-react-query)

## 1.2 Nada de fetch manual em onMounted/setInterval — use query

**Impacto: CRITICAL (cache, dedupe, loading/erro e invalidação de graça)**

Fetch manual com `ref` + `onMounted` não participa do cache: a mutation de outra tela não atualiza esse componente, não há dedupe, e o `setInterval` vaza se esquecer o cleanup. A regra do repo é clara: **componentes consomem hooks/queries, nunca o service direto**.

**Incorreto (`components/dashboard/UpcomingAppointments.vue`, `views/RecebimentosOrfaosView.vue` — não copie):**

```ts
const service = AgendamentoService()
const items = ref<AgendamentoResponse[]>([])
const loading = ref(false)

async function load() {
  loading.value = true
  items.value = (await service.getByFilters({ pageNumber: 1, pageSize: 5 })).items
  loading.value = false
}
onMounted(() => { load(); setInterval(load, 120_000) }) // interval nunca limpo
```

**Correto:**

```ts
// src/queries/agendamento.queries.ts
export function useProximosAgendamentosQuery() {
  return useQuery({
    queryKey: [...agendamentoKeys.lists(), 'proximos'],
    queryFn: ({ signal }) => service.getByFilters({ pageNumber: 1, pageSize: 5 }, signal),
    refetchInterval: 120_000, // pausa sozinho com a aba em background
  })
}

// componente
const { data, isLoading } = useProximosAgendamentosQuery()
```

Exceção legítima: exportação CSV de "tudo" — mesmo assim, chame via função do hook (`useAgendamento().exportarCsv`) ou `queryClient.fetchQuery`, não o service direto na view.

Referência: [Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/queries) · [Why You Want React Query — TkDodo](https://tkdodo.eu/blog/why-you-want-react-query)

## 1.3 Query keys e parâmetros reativos (MaybeRef + unref)

**Impacto: CRITICAL (lista passa a reagir a filtro/paginação sem refetch manual)**

No Vue Query, a `queryKey` é observada profundamente: se você passar um `ref`/`computed` **dentro** da key, a query refaz o fetch sozinha quando o valor muda. Se você passar `.value`, a key é avaliada uma única vez na criação e congela.

**Incorreto (key avaliada uma vez — `src/queries/empresa.queries.ts`, não copie):**

```ts
export function useEmpresaQuery() {
  const empresaId = computed(() => auth.empresaId)
  return useQuery({
    queryKey: [empresaQueryKey, empresaId.value], // congela no valor inicial
    queryFn: () => service.getById(empresaId.value!),
  })
}
```

**Correto (padrão de referência — `src/queries/usuario.queries.ts`):**

```ts
export function useUsuariosQuery(params: MaybeRef<UsuarioListQueryDto>) {
  return useQuery({
    queryKey: [usuarioQueryKey, params],          // ref dentro da key
    queryFn: async () => await service.getPaged(unref(params)),
    placeholderData: keepPreviousData,
  })
}

// na view
const params = computed<UsuarioListQueryDto>(() => ({
  pageNumber: page.value,
  pageSize: pageSize.value,
  search: searchDebounced.value || undefined,
}))
const { data, isLoading } = useUsuariosQuery(params)
```

Regra prática: toda função `useXxxQuery` recebe `MaybeRef<T>` (ou `MaybeRefOrGetter<T>` + `toValue`) e nunca `T` cru.

Referência: [TanStack Vue Query — Reactivity](https://tanstack.com/query/latest/docs/framework/vue/reactivity)

## 1.4 enabled reativo (getter), nunca booleano estático

**Impacto: HIGH (detalhe carrega quando o id chega; evita fetch com id vazio)**

`enabled: !!id` com `id: string` é avaliado **uma vez**. Se o componente monta antes do id existir (rota carregando, item selecionado depois), a query nunca habilita — ou fica presa no id antigo.

**Incorreto (`fornecedor`, `produto`, `grade`, `agendamento`, `unidadeEntrega`, `localDescarga` — não copie):**

```ts
export function useFornecedorByIdQuery(id: string) {
  return useQuery({
    queryKey: [fornecedorQueryKey, id],
    queryFn: async () => await service.getById(id),
    enabled: !!id, // estático
  })
}
```

**Correto (`src/queries/usuario.queries.ts`):**

```ts
export function useUsuarioByIdQuery(id: MaybeRef<string | undefined | null>) {
  return useQuery({
    queryKey: [usuarioQueryKey, id],
    queryFn: async () => await service.getById(unref(id)!),
    enabled: () => !!unref(id),
  })
}
```

Escolha **uma** forma por módulo: getter `() => !!unref(id)` (preferida) ou `computed(() => !!unref(id))`.

Referência: [Dependent Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/dependent-queries)

## 1.5 QueryClient com defaults globais e QueryCache.onError

**Impacto: HIGH (erro de query nunca mais fica silencioso; retry sem martelar 4xx)**

Hoje `main.ts` faz `app.use(VueQueryPlugin)` sem opções: queries falhas não mostram nada ao usuário, e o retry padrão (3x) refaz até 403/404 — que nunca vão mudar.

**Incorreto (`src/main.ts`, não copie):**

```ts
app.use(VueQueryPlugin)
```

**Correto:**

```ts
// src/plugins/vue-query.ts
import { QueryCache, QueryClient, type VueQueryPluginOptions } from '@tanstack/vue-query'
import { isAxiosError } from 'axios'
import { useToastStore } from '@/stores/ToastStore'
import { getErrorMessage } from '@/http/errors'

export const queryClient = new QueryClient({
  queryCache: new QueryCache({
    onError: (error, query) => {
      if (query.meta?.silent) return
      // só toast quando já havia dado (refetch em background falhou) ou meta pedir
      useToastStore().notify(getErrorMessage(error, 'Erro ao carregar dados.'), 'error', 5000)
    },
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (count, error) => {
        const status = isAxiosError(error) ? error.response?.status : undefined
        if (status && status >= 400 && status < 500) return false // 4xx não melhora com retry
        return count < 2
      },
      refetchOnWindowFocus: true,
    },
    mutations: { retry: false },
  },
})

export const vueQueryOptions: VueQueryPluginOptions = { queryClient }

// main.ts
app.use(VueQueryPlugin, vueQueryOptions)
```

`mutations.retry: false` é obrigatório: mutation de reserva/check-in não pode ser repetida automaticamente sem Idempotency-Key (ADR-0004, item 12).

Referência: [QueryClient](https://tanstack.com/query/latest/docs/reference/QueryClient) · [React Query Error Handling — TkDodo](https://tkdodo.eu/blog/react-query-error-handling)

## 1.6 keepPreviousData em toda listagem paginada/filtrada

**Impacto: HIGH (elimina o "pisca" de skeleton ao trocar de página ou filtro)**

Toda listagem de tela é paginada server-side (`<Entidade>ListQueryDto` → `PaginatedResponse<T>`). Sem `placeholderData: keepPreviousData`, cada troca de página vira `data = undefined` → skeleton → dados, e a `v-pagination` some e volta.

**Incorreto:**

```ts
return useQuery({
  queryKey: [gradeQueryKey, params],
  queryFn: () => service.getPaged(unref(params)),
})
```

**Correto:**

```ts
import { keepPreviousData, useQuery } from '@tanstack/vue-query'

return useQuery({
  queryKey: [gradeQueryKey, params],
  queryFn: () => service.getPaged(unref(params)),
  placeholderData: keepPreviousData,
})
```

Na view, skeleton só quando **não há nada** (`v-if="isLoading && !data"`) e um indicador leve (`<v-progress-linear indeterminate v-if="isFetching" />`) durante a troca.

Referência: [Paginated Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/paginated-queries)

## 1.7 Query key factory por entidade

**Impacto: HIGH (invalidação previsível de lista + detalhe, sem string solta)**

Hoje o repo usa uma constante string (`fornecedorQueryKey = 'fornecedores'`) e, em alguns módulos, uma key **diferente** para o detalhe (`["agendamento", id]` vs `["agendamentos"]`). Resultado: a mutation invalida a lista e o detalhe fica velho. A solução, popularizada por TkDodo (*Effective React Query Keys*, vale 1:1 para Vue Query), é uma factory hierárquica por entidade.

**Incorreto (keys divergentes — `src/hooks/useAgendamento.ts`, não copie):**

```ts
// queries: ["agendamentos", filtros] e ["agendamento", id]
onSuccess: () => queryClient.invalidateQueries({ queryKey: ["agendamentos"] })
// o detalhe ["agendamento", id] NÃO é invalidado
```

**Correto (factory hierárquica):**

```ts
// src/queries/agendamento.queries.ts
export const agendamentoKeys = {
  all: ['agendamentos'] as const,
  lists: () => [...agendamentoKeys.all, 'list'] as const,
  list: (params: MaybeRef<IAgendamentoFilterDto>) => [...agendamentoKeys.lists(), params] as const,
  details: () => [...agendamentoKeys.all, 'detail'] as const,
  detail: (id: MaybeRef<string | null | undefined>) => [...agendamentoKeys.details(), id] as const,
}

// mutation: uma invalidação cobre lista E detalhe
onSuccess: () => queryClient.invalidateQueries({ queryKey: agendamentoKeys.all })
```

Ao migrar um módulo, mantenha o primeiro segmento igual ao atual (`'agendamentos'`) para não quebrar invalidações feitas por outros hooks/views (ex.: `GradeView` invalida `['agendamentos']`).

Referência: [Effective React Query Keys — TkDodo](https://tkdodo.eu/blog/effective-react-query-keys) · [Query Keys](https://tanstack.com/query/latest/docs/framework/vue/guides/query-keys)

## 1.8 Mensagem de erro do backend no toast (com fallback)

**Impacto: HIGH (usuário vê "Slot já reservado" em vez de "Erro ao salvar")**

O backend devolve mensagens de negócio em PT-BR (`BusinessException`, `NotFoundException`). Alguns hooks já leem `error?.response?.data?.message` (useAgendamento, useGrade); outros jogam fora e mostram texto fixo. Centralize a extração num helper tipado.

**Incorreto (`src/hooks/useFornecedor.ts`, não copie):**

```ts
onError: () => {
  toast.notify("Erro ao cadastrar", "error")
}
```

**Incorreto 2 (`any` espalhado):**

```ts
onError: (error: any) => toast.notify(error?.response?.data?.message ?? "Erro", "error")
```

**Correto:**

```ts
// src/http/errors.ts
import { isAxiosError } from 'axios'

type ApiErrorBody = { message?: string; title?: string; detail?: string; errors?: Record<string, string[]> }

export function getErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError<ApiErrorBody>(error)) return fallback
  const body = error.response?.data
  if (body?.errors) return Object.values(body.errors).flat()[0] ?? fallback // ModelState/ValidationProblem
  return body?.message ?? body?.detail ?? body?.title ?? fallback
}

// hook
onError: (error) => toast.notify(getErrorMessage(error, 'Erro ao cadastrar fornecedor.'), 'error')
```

Confira o shape real do envelope de erro no middleware de exceções do backend antes de ajustar `ApiErrorBody`.

Referência: [Mutations](https://tanstack.com/query/latest/docs/framework/vue/guides/mutations) · [axios isAxiosError](https://axios-http.com/docs/handling_errors)

## 1.9 staleTime por tipo de recurso

**Impacto: MEDIUM-HIGH (corta refetch desnecessário de dados quase estáticos)**

O default é `staleTime: 0`: todo mount e todo foco de janela refaz o fetch. Para dados operacionais (agendamentos do dia, fila da portaria) isso é bom. Para cadastro mestre (roles, produtos, locais de descarga, unidades) é desperdício — hoje só `useRolesQuery` define `staleTime`.

| Recurso | staleTime sugerido |
|---|---|
| Roles, enums, tipos de veículo | `Infinity` ou 30 min |
| Cadastros mestres (produto, fornecedor, local, unidade) | 5 min |
| Listagens administrativas (usuários, grades) | 30 s |
| Operacional (agendamentos do dia, fila, notificações) | 0 + SSE/`refetchInterval` |

**Incorreto (cadastro mestre refetchado a cada abertura de modal):**

```ts
export function useProdutosQuery() {
  return useQuery({ queryKey: [produtoQueryKey], queryFn: () => service.getAll() })
}
```

**Correto:**

```ts
export function useProdutosQuery() {
  return useQuery({
    queryKey: [produtoQueryKey],
    queryFn: () => service.getAll(),
    staleTime: 5 * 60 * 1000,
  })
}
```

`staleTime` não esconde mudança feita **por você**: a mutation invalida e o dado é refetchado na hora. Só evita refetch "por precaução".

Referência: [Important Defaults](https://tanstack.com/query/latest/docs/framework/vue/guides/important-defaults) · [React Query as a State Manager — TkDodo](https://tkdodo.eu/blog/react-query-as-a-state-manager)

## 1.10 Update otimista com rollback para toggles de status

**Impacto: MEDIUM (switch ativo/inativo responde instantâneo; volta se o servidor recusar)**

Para ações binárias e reversíveis (ativar/inativar usuário, local de descarga, marcar notificação como lida), atualize o cache antes da resposta e faça rollback no erro. **Não** use otimista em ações com regra de negócio forte no backend (reservar slot, check-in, cancelar agendamento): ali o servidor decide.

**Incorreto (switch fica "travado" até a resposta e pisca):**

```ts
const setStatusMutation = useMutation({
  mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => service.setStatus(id, ativo),
  onSuccess: () => invalidate(),
})
```

**Correto:**

```ts
const setStatusMutation = useMutation({
  mutationFn: ({ id, ativo }: { id: string; ativo: boolean }) => service.setStatus(id, ativo),
  onMutate: async ({ id, ativo }) => {
    await queryClient.cancelQueries({ queryKey: [usuarioQueryKey] })
    const snapshots = queryClient.getQueriesData<PaginatedResponse<UsuarioResponseDto>>({
      queryKey: [usuarioQueryKey],
    })
    queryClient.setQueriesData<PaginatedResponse<UsuarioResponseDto>>(
      { queryKey: [usuarioQueryKey] },
      (old) => old && { ...old, items: old.items.map((u) => (u.id === id ? { ...u, deletedAt: ativo ? null : new Date().toISOString() } : u)) },
    )
    return { snapshots }
  },
  onError: (error, _vars, ctx) => {
    ctx?.snapshots.forEach(([key, data]) => queryClient.setQueryData(key, data))
    toast.notify(getErrorMessage(error, 'Erro ao alterar status do usuário.'), 'error')
  },
  onSettled: () => queryClient.invalidateQueries({ queryKey: [usuarioQueryKey] }),
})
```

Referência: [Optimistic Updates](https://tanstack.com/query/latest/docs/framework/vue/guides/optimistic-updates) · [Concurrent Optimistic Updates — TkDodo](https://tkdodo.eu/blog/concurrent-optimistic-updates-in-react-query)

## 1.11 Prefetch do detalhe na intenção (hover/foco)

**Impacto: MEDIUM (modal/drawer de detalhe abre com dado já em cache)**

Em listas onde o usuário abre detalhe com frequência (agendamento → conferência, usuário → `AuditDrawer`), prefetch no `mouseenter`/`focus` do card economiza o round-trip percebido.

**Incorreto (detalhe só começa a carregar depois do clique):**

```vue
<v-card @click="abrir(item.id)">...</v-card>
```

**Correto:**

```ts
const queryClient = useQueryClient()

function prefetchDetalhe(id: string) {
  queryClient.prefetchQuery({
    queryKey: agendamentoKeys.detail(id),
    queryFn: () => service.getById(id),
    staleTime: 30_000, // não refaz se já está fresco
  })
}
```

```vue
<v-card
  @mouseenter="prefetchDetalhe(item.id)"
  @focus="prefetchDetalhe(item.id)"
  @click="abrir(item.id)"
>
```

Use a **mesma** key e queryFn do `useXxxByIdQuery` (por isso a key factory). Não prefetch em lista de 100 itens no mount — só na intenção.

Referência: [Prefetching](https://tanstack.com/query/latest/docs/framework/vue/guides/prefetching)

## 1.12 Derive dados com select, não com cópia em ref

**Impacto: MEDIUM (dado derivado sempre sincronizado com o cache)**

Copiar `data` para um `ref` local via `watch` cria duas fontes de verdade: quando a query refetcha, a cópia fica para trás. Para transformar (opções de `v-select`, séries de gráfico), use `select` na query ou `computed` sobre `data`.

**Incorreto:**

```ts
const { data } = useLocaisDescargaQuery()
const opcoes = ref<{ title: string; value: string }[]>([])
watch(data, (v) => {
  opcoes.value = (v ?? []).map((l) => ({ title: l.nome, value: l.id }))
})
```

**Correto (select — o componente só re-renderiza se o resultado mudar):**

```ts
export function useLocaisDescargaOptionsQuery() {
  return useQuery({
    queryKey: [localDescargaQueryKey, 'options'],
    queryFn: () => service.getAll(),
    select: (locais) => locais.filter((l) => l.ativo).map((l) => ({ title: l.nome, value: l.id })),
    staleTime: 5 * 60 * 1000,
  })
}
```

**Correto (computed, quando a derivação depende de estado da view):**

```ts
const opcoes = computed(() => (data.value ?? []).map((l) => ({ title: l.nome, value: l.id })))
```

Referência: [React Query Data Transformations — TkDodo](https://tkdodo.eu/blog/react-query-data-transformations)


---

# 2. Eliminando waterfalls

**Impacto da seção:** CRITICAL

## 2.1 Evite waterfall de queries dependentes desnecessárias

**Impacto: CRITICAL (elimina um round-trip inteiro por nível de dependência)**

Queries com `enabled` encadeado (A → B → C) carregam em série. Só encadeie quando B realmente precisa de um dado que **só** A tem. Se o id já está na rota, dispare tudo em paralelo.

**Incorreto (conferência espera o agendamento só para ler um id que já está na URL):**

```ts
const agendamentoId = computed(() => route.params.agendamentoId as string)
const { data: agendamento } = useAgendamentoByIdQuery(agendamentoId)
const conferenciaId = computed(() => agendamento.value?.id)
const { data: conferencia } = useConferenciaQuery(conferenciaId) // espera o primeiro
```

**Correto (as duas queries partem juntas):**

```ts
const agendamentoId = computed(() => route.params.agendamentoId as string)
const { data: agendamento } = useAgendamentoByIdQuery(agendamentoId)
const { data: conferencia } = useConferenciaQuery(agendamentoId)
```

Se a tela sempre precisa de A+B juntos, considere um endpoint agregado no backend (`GET /agendamentos/{id}/conferencia-completa`) — um round-trip, uma transação, um filtro de tenant.

Referência: [Dependent Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/dependent-queries) · [Parallel Queries](https://tanstack.com/query/latest/docs/framework/vue/guides/parallel-queries)

## 2.2 Promise.all para operações independentes

**Impacto: CRITICAL (2-5x mais rápido em telas que carregam várias fontes)**

Quando operações assíncronas não dependem uma da outra, rode-as em paralelo. Vale para exportação CSV, prefetch de várias entidades e loops de update (ex.: salvar várias linhas da grade).

**Incorreto (N round-trips sequenciais — padrão de `views/GradeView.vue`, não copie):**

```ts
for (const g of gradesAlteradas) {
  await GradeService().updateGrade(g.id, g) // cada update espera o anterior
}
```

**Correto (paralelo, dentro do hook):**

```ts
// src/hooks/useGrade.ts
const updateManyMutation = useMutation({
  mutationFn: (grades: GradeUpdateDto[]) =>
    Promise.all(grades.map((g) => service.updateGrade(g.id, g))),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: gradeKeys.all }),
})
```

Se o backend tiver regra de concorrência entre as linhas, prefira um endpoint em lote (`PATCH /Grade/lote`) a N chamadas — uma transação no servidor é mais segura que N no cliente. Para mais de ~10 chamadas, limite a concorrência (chunks) para não estourar o rate limit.

Referência: [MDN Promise.all](https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/Promise/all)

## 2.3 Repasse o AbortSignal do queryFn para o axios

**Impacto: MEDIUM-HIGH (busca com debounce cancela a requisição anterior de verdade)**

O Vue Query passa um `signal` para o `queryFn`. Se o service repassar para o axios, trocar o filtro/página cancela a requisição em voo — menos carga no backend e nenhuma resposta antiga chegando "atrasada".

**Incorreto (service ignora o signal):**

```ts
const getPaged = async (query: UsuarioListQueryDto) => {
  const { data } = await http.get('/usuarios', { params: query })
  return data
}
```

**Correto:**

```ts
// service
const getPaged = async (
  query: UsuarioListQueryDto,
  signal?: AbortSignal,
): Promise<PaginatedResponse<UsuarioResponseDto>> => {
  const { data } = await http.get<PaginatedResponse<UsuarioResponseDto>>('/usuarios', { params: query, signal })
  return data
}

// query
queryFn: ({ signal }) => service.getPaged(unref(params), signal),
```

Cuidado: o interceptor de refresh em `src/http/http.ts` reexecuta `http(original)` — o `signal` vai junto no config, então um request cancelado não é refeito. Não passe `signal` em mutations.

Referência: [Query Cancellation](https://tanstack.com/query/latest/docs/framework/vue/guides/query-cancellation) · [axios cancellation](https://axios-http.com/docs/cancellation)


---

# 3. Tamanho de bundle

**Impacto da seção:** CRITICAL

## 3.1 Toda rota autenticada é lazy (import dinâmico)

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

## 3.2 Vuetify com tree-shaking (vite-plugin-vuetify), não import * as components

**Impacto: CRITICAL (corta centenas de KB do bundle inicial)**

Registrar **todos** os componentes e diretivas do Vuetify coloca a biblioteca inteira no bundle, mesmo o que nunca é usado. O `vite-plugin-vuetify` com `autoImport` importa só o que aparece nos templates.

**Incorreto (`src/main.ts` hoje, não copie):**

```ts
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
const vuetify = createVuetify({ components, directives })
```

**Correto:**

```ts
// vite.config.ts
import vuetify from 'vite-plugin-vuetify'
export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true }), vueDevTools()],
})

// src/plugins/vuetify.ts
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { truckflowTheme } from '@/theme/truckflow'
export default createVuetify({
  theme: { defaultTheme: 'truckflow', themes: { truckflow: truckflowTheme } },
})
```

Depois da troca, rode `pnpm build` e compare o tamanho do chunk `index-*.js` (script `bundle_analyzer.py` da skill `senior-frontend`). Componentes usados só via `h()`/render function precisam de import explícito.

Referência: [Vuetify — Treeshaking](https://vuetifyjs.com/en/features/treeshaking/) · [vite-plugin-vuetify](https://github.com/vuetifyjs/vuetify-loader/tree/master/packages/vite-plugin)

## 3.3 defineAsyncComponent para dialogs e gráficos pesados

**Impacto: HIGH (ApexCharts e modais raros saem do chunk da tela)**

`app.use(VueApexCharts)` em `main.ts` coloca o ApexCharts no bundle **inicial**, inclusive na tela de login. Modais grandes que abrem raramente (`RelatorioDialog`, `AgendamentoAvulsoModal`, `AuditDrawer`) também não precisam vir no chunk da view.

**Incorreto:**

```ts
// main.ts
import VueApexCharts from 'vue3-apexcharts'
app.use(VueApexCharts)
```

**Correto (gráfico carregado só no dashboard):**

```ts
// components/dashboard/StatusChart.vue
import { defineAsyncComponent } from 'vue'
const ApexChart = defineAsyncComponent(() => import('vue3-apexcharts'))
```

```vue
<ApexChart type="donut" :options="options" :series="series" height="280" />
```

**Correto (modal raro):**

```ts
const RelatorioDialog = defineAsyncComponent(() => import('@/components/modals/RelatorioDialog.vue'))
```

```vue
<RelatorioDialog v-if="relatorioAberto" v-model="relatorioAberto" />
```

O `v-if` garante que o chunk só é baixado quando o usuário abre o modal.

Referência: [Vue — Async Components](https://vuejs.org/guide/components/async.html)

## 3.4 Ícones MDI — font inteira vs @mdi/js por ícone

**Impacto: MEDIUM (~300KB de font/CSS a menos quando migrado para SVG)**

`@mdi/font` baixa a fonte com os ~7.000 ícones para usar dezenas. Com `@mdi/js` + iconset `mdi-svg`, só os ícones importados entram no bundle. É uma migração grande (todo `prepend-icon="mdi-..."` vira binding) — faça por tela, não num PR gigante, e só se o bundle for um problema medido.

**Atual (aceitável, mas pesado):**

```vue
<v-btn prepend-icon="mdi-truck-check">Check-in</v-btn>
```

**Alvo:**

```ts
// src/plugins/vuetify.ts
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'
createVuetify({ icons: { defaultSet: 'mdi', aliases, sets: { mdi } } })
```

```vue
<script setup lang="ts">
import { mdiTruckCheck } from '@mdi/js'
</script>
<template>
  <v-btn :prepend-icon="mdiTruckCheck">Check-in</v-btn>
</template>
```

Não misture os dois sistemas na mesma tela. Decida no nível do projeto.

Referência: [Vuetify — Icon Fonts](https://vuetifyjs.com/en/features/icon-fonts/)


---

# 4. Reatividade

**Impacto da seção:** HIGH

## 4.1 Estado derivado é computed, não watch + ref

**Impacto: HIGH (uma fonte de verdade, sem dessincronização)**

Se um valor pode ser calculado a partir de outros, ele é `computed`. `watch` serve para **efeitos colaterais** (sincronizar URL, chamar API imperativa, foco) — não para manter cópias.

**Incorreto:**

```ts
const totalPages = ref(1)
watch(data, (v) => {
  totalPages.value = Math.ceil((v?.totalCount ?? 0) / pageSize.value)
})
```

**Correto:**

```ts
const totalPages = computed(() => data.value?.totalPages ?? 1) // o backend já calcula
```

**Uso legítimo de watch (efeito colateral — `ManageUserView.vue`):**

```ts
watch([searchDebounced, filtroStatus, page], () => {
  router.replace({ query: { search: searchDebounced.value || undefined, page: page.value !== 1 ? String(page.value) : undefined } })
})
```

`computed` deve ser puro: sem chamada HTTP, sem mutação de outro estado.

Referência: [Vue — Computed Properties](https://vuejs.org/guide/essentials/computed.html) · [Vue — Watchers](https://vuejs.org/guide/essentials/watchers.html)

## 4.2 Não perca reatividade ao desestruturar (props, reactive, stores)

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

## 4.3 Cleanup em watchers e efeitos (onWatcherCleanup / onScopeDispose)

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

## 4.4 shallowRef/markRaw para listas grandes e objetos de terceiros

**Impacto: MEDIUM (evita proxy profundo em milhares de objetos)**

`ref`/`reactive` tornam tudo profundamente reativo. Para listas grandes que você substitui inteiras (export CSV com `pageSize: 1000`, dados de gráfico) ou instâncias de bibliotecas (chart, EventSource, AbortController), a reatividade profunda é custo sem benefício.

Dados vindos do Vue Query já são tratados pela lib — esta regra vale para estado **local**.

**Incorreto:**

```ts
const linhasExport = ref<AgendamentoResponse[]>([]) // 1000 objetos com proxy profundo
const chartInstance = ref<ApexCharts | null>(null)
```

**Correto:**

```ts
const linhasExport = shallowRef<AgendamentoResponse[]>([])
linhasExport.value = resultado.items // substituir inteiro dispara a reatividade

const chartInstance = shallowRef<ApexCharts | null>(null)
```

Com `shallowRef`, `linhasExport.value.push(x)` **não** dispara update — substitua o array (`linhasExport.value = [...linhasExport.value, x]`) ou chame `triggerRef`.

Referência: [Vue — Reducing Reactivity Overhead for Large Immutable Structures](https://vuejs.org/guide/best-practices/performance.html#reduce-reactivity-overhead-for-large-immutable-structures)

## 4.5 Exponha estado read-only de composables e stores

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


---

# 5. Renderização

**Impacto da seção:** MEDIUM

## 5.1 key estável (id do backend) em todo v-for

**Impacto: HIGH (evita estado de linha trocado e re-render total da lista)**

`:key="index"` faz o Vue reaproveitar o DOM pela posição: ao paginar, filtrar ou remover um item, o estado interno (menu aberto, input digitado, loading do botão) "pula" para o item errado. Toda entidade do backend tem `id: string` (Guid) — use.

**Incorreto:**

```vue
<v-card v-for="(u, i) in data?.items" :key="i">
```

**Correto:**

```vue
<v-card v-for="u in data?.items" :key="u.id">
```

Também use `:key` para **resetar** um componente de formulário entre criar/editar, como `ManageUserView` faz: `<UsuarioFormModal :key="editingUser?.id ?? 'new'" ... />` — mais simples e seguro que `watch(initialData)` + `Object.assign`.

Referência: [Vue — Maintaining State with key](https://vuejs.org/guide/essentials/list.html#maintaining-state-with-key)

## 5.2 v-memo em listas grandes, v-once em conteúdo estático

**Impacto: MEDIUM (pula re-render de linhas que não mudaram)**

Em listas com centenas de itens onde um estado global muda (ex.: id selecionado, filtro de destaque), todas as linhas re-renderizam. `v-memo` com as dependências da linha pula as que não mudaram. **Meça antes** — com listas paginadas de 12-50 itens, normalmente não compensa.

**Incorreto (todas as linhas re-renderizam ao trocar a seleção):**

```vue
<div v-for="a in agendamentos" :key="a.id" :class="{ selecionado: a.id === selecionadoId }">
  <AgendamentoCard :agendamento="a" />
</div>
```

**Correto:**

```vue
<div
  v-for="a in agendamentos"
  :key="a.id"
  v-memo="[a.id === selecionadoId, a.status, a.updatedAt]"
  :class="{ selecionado: a.id === selecionadoId }"
>
  <AgendamentoCard :agendamento="a" />
</div>
```

`v-once` para blocos que nunca mudam após o mount (termos LGPD, texto institucional de `SaibaMais.vue`).

Se esquecer uma dependência no `v-memo`, a linha fica desatualizada — é uma otimização com risco de bug; documente o porquê.

Referência: [Vue — v-memo](https://vuejs.org/api/built-in-directives.html#v-memo)

## 5.3 v-if para o raro e caro, v-show para o que alterna muito

**Impacto: MEDIUM (dialogs pesados não montam até abrir; tabs não remontam)**

- `v-if`: não monta nada enquanto falso. Use em **dialogs/drawers** pesados com queries dentro (`AuditDrawer`, `RelatorioDialog`) — senão a query do drawer roda já no mount da página.
- `v-show`: monta uma vez e só alterna `display`. Use em painéis/abas que o usuário troca toda hora.

**Incorreto (drawer de auditoria dispara query de auditoria assim que a tela abre):**

```vue
<AuditDrawer v-model="auditOpen" entity-name="Administrador" :entity-id="auditId" />
```

**Correto:**

```vue
<AuditDrawer v-if="auditId" v-model="auditOpen" entity-name="Administrador" :entity-id="auditId" />
```

Alternativa: deixar montado e garantir `enabled: () => open.value && !!unref(id)` na query do drawer.

Referência: [Vue — v-if vs. v-show](https://vuejs.org/guide/essentials/conditional.html#v-if-vs-v-show)

## 5.4 Virtualize listas longas não paginadas

**Impacto: MEDIUM (DOM constante independente do tamanho da lista)**

A regra do projeto é **paginar server-side** toda listagem de tela. Mas há listas que legitimamente vêm inteiras: itens de uma NF-e, timeline de auditoria de uma entidade, seleção de produtos (`ProdutoSelectModal`). Acima de ~200 linhas, virtualize.

**Incorreto (500 itens de NF-e renderizados de uma vez):**

```vue
<v-data-table :items="itensNota" :headers="headers" :items-per-page="-1" />
```

**Correto:**

```vue
<v-data-table-virtual :items="itensNota" :headers="headers" height="480" fixed-header />
```

**Lista simples:**

```vue
<v-virtual-scroll :items="produtos" height="400" item-height="48">
  <template #default="{ item }">
    <v-list-item :key="item.id" :title="item.nome" :subtitle="item.codigo" />
  </template>
</v-virtual-scroll>
```

Virtualização **não** substitui paginação server-side: se a lista pode crescer sem limite (agendamentos, usuários), ela é paginada.

Referência: [Vuetify — Virtual tables](https://vuetifyjs.com/en/components/data-tables/virtual-tables/) · [Vuetify — v-virtual-scroll](https://vuetifyjs.com/en/components/virtual-scroller/)

## 5.5 KeepAlive seletivo para telas de ida-e-volta

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


---

# 6. API de componentes

**Impacto da seção:** MEDIUM

## 6.1 defineProps e defineEmits tipados (tupla), nunca array de strings nem any

**Impacto: HIGH (contrato quebrado vira erro no vue-tsc — que roda no CI —, não bug em produção)**

O CI roda `vue-tsc --build`. Isso só protege o que está tipado. `defineEmits(["close", "submit"])` e `initialData?: any` desligam a checagem justamente no contrato form ↔ view.

**Incorreto (`FornecedorFormModal.vue`, `ProdutoFormModal.vue`, `LocalDescargaFormModal.vue` — não copie):**

```ts
const props = defineProps<{ open: boolean; initialData?: any }>()
const emit = defineEmits(["close", "submit"])
const formRef = ref<any>(null)
```

**Correto (`UsuarioFormModal.vue`, com o `formRef` tipado):**

```ts
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  open: boolean
  initialData?: FornecedorResponse
  loading?: boolean
}>()

const emit = defineEmits<{
  close: []
  submit: [payload: FornecedorCreateDto | FornecedorUpdateDto]
}>()

const formRef = ref<VForm | null>(null)

async function onSubmit() {
  const result = await formRef.value?.validate()
  if (!result?.valid) return
  emit('submit', { ...form, cnpj: form.cnpj.replace(/\D/g, '') })
}
```

Defaults: `withDefaults(defineProps<Props>(), { loading: false })` ou, em 3.5, destructure com default: `const { loading = false } = defineProps<Props>()`.

Referência: [Vue — Typing Component Props](https://vuejs.org/guide/typescript/composition-api.html#typing-component-props) · [Typing Component Emits](https://vuejs.org/guide/typescript/composition-api.html#typing-component-emits)

## 6.2 defineModel para v-model de dialogs e inputs (Vue 3.4+)

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

## 6.3 provide/inject com InjectionKey tipado

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

## 6.4 Tipar slots de componentes genéricos (defineSlots + generic)

**Impacto: LOW-MEDIUM (tabela/lista genérica com autocomplete do item no slot)**

Componentes reutilizáveis de listagem (o `CrudTable.vue` atual, ou um futuro `PagedList.vue`) recebem itens de qualquer entidade. Com `generic` e `defineSlots`, o slot conhece o tipo do item.

**Incorreto:**

```ts
const props = defineProps<{ items: any[]; headers: any[] }>()
```

**Correto:**

```vue
<script setup lang="ts" generic="T extends { id: string }">
const props = defineProps<{
  items: T[]
  loading?: boolean
  totalPages: number
}>()
const page = defineModel<number>('page', { required: true })

defineSlots<{
  item(props: { item: T }): any
  empty(): any
}>()
</script>

<template>
  <v-skeleton-loader v-if="loading && !items.length" type="card@3" />
  <template v-else-if="items.length">
    <template v-for="item in items" :key="item.id">
      <slot name="item" :item="item" />
    </template>
    <v-pagination v-model="page" :length="totalPages" :total-visible="7" />
  </template>
  <slot v-else name="empty" />
</template>
```

Referência: [Vue — Generics](https://vuejs.org/api/sfc-script-setup.html#generics) · [defineSlots](https://vuejs.org/api/sfc-script-setup.html#defineslots)


---

# 7. Composables

**Impacto da seção:** MEDIUM

## 7.1 useUrlQueryState — filtros e página sincronizados com route.query

**Impacto: MEDIUM-HIGH (filtro/paginação sobrevive a refresh e link compartilhado, sem copiar código)**

Convenção do projeto: listagens refletem filtros e página em `route.query`. Hoje cada view reimplementa: lê `route.query` no init, `watch` para `router.replace`, reset da página quando filtro muda. Extraia.

**Incorreto (copiado em cada listagem):**

```ts
const page = ref(Number(route.query.page) || 1)
const search = ref(route.query.search?.toString() ?? "")
watch([searchDebounced, filtroStatus], () => { page.value = 1 })
watch([searchDebounced, filtroStatus, page], () => {
  router.replace({ query: { search: searchDebounced.value || undefined, page: page.value !== 1 ? String(page.value) : undefined } })
})
```

**Correto:**

```ts
// src/composables/useListQueryState.ts
import { computed, ref, watch, type Ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDebouncedRef } from './useDebouncedRef'

export function useListQueryState<F extends Record<string, string | undefined>>(defaults: F, pageSize = 12) {
  const route = useRoute()
  const router = useRouter()

  const page = ref(Number(route.query.page) || 1)
  const search = ref(route.query.search?.toString() ?? '')
  const searchDebounced = useDebouncedRef(search)
  const filters = ref({ ...defaults, ...pick(route.query, Object.keys(defaults)) }) as Ref<F>

  watch([searchDebounced, filters], () => { page.value = 1 }, { deep: true })
  watch([searchDebounced, filters, page], () => {
    router.replace({
      query: {
        ...clean(filters.value, defaults),
        search: searchDebounced.value || undefined,
        page: page.value !== 1 ? String(page.value) : undefined,
      },
    })
  }, { deep: true })

  const params = computed(() => ({
    pageNumber: page.value,
    pageSize,
    search: searchDebounced.value || undefined,
    ...clean(filters.value, defaults),
  }))

  return { page, search, filters, params }
}

function pick(q: Record<string, unknown>, keys: string[]) {
  return Object.fromEntries(keys.filter((k) => typeof q[k] === 'string').map((k) => [k, q[k] as string]))
}
function clean<F extends Record<string, string | undefined>>(f: F, d: F) {
  return Object.fromEntries(Object.entries(f).filter(([k, v]) => v && v !== d[k])) as Partial<F>
}

// view
const { page, search, filters, params } = useListQueryState({ status: 'all' })
const { data, isLoading } = useUsuariosQuery(params)
```

`router.replace` (não `push`) para não poluir o histórico a cada tecla.

Referência: [Vue Router — Programmatic Navigation](https://router.vuejs.org/guide/essentials/navigation.html)

## 7.2 Composables limpam o que criam (onScopeDispose)

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

## 7.3 Extraia useDebouncedRef (debounce de busca único no projeto)

**Impacto: MEDIUM (um debounce consistente com cleanup, em vez de 3 cópias)**

O debounce de busca está copiado em `ManageUserView` (400ms), `VisualizarAgendamentoView` (350ms) e `RecebimentosOrfaosView` (350ms), cada um com `let timeout` sem cleanup. A convenção do projeto é **~350ms, server-side**.

**Incorreto (copiado em cada view):**

```ts
let timeout: ReturnType<typeof setTimeout>
watch(search, (val) => {
  clearTimeout(timeout)
  timeout = setTimeout(() => { searchDebounced.value = val ?? "" }, 400)
})
```

**Correto:**

```ts
// src/composables/useDebouncedRef.ts
import { onWatcherCleanup, ref, watch, type Ref } from 'vue'

export function useDebouncedRef<T>(source: Ref<T>, delay = 350): Readonly<Ref<T>> {
  const debounced = ref(source.value) as Ref<T>
  watch(source, (val) => {
    const t = setTimeout(() => { debounced.value = val }, delay)
    onWatcherCleanup(() => clearTimeout(t))
  })
  return debounced
}

// view
const search = ref(route.query.search?.toString() ?? '')
const searchDebounced = useDebouncedRef(search)
```

Alternativa madura: `refDebounced` do VueUse (Anthony Fu). Adicionar a dependência é decisão do time — o composable acima tem 10 linhas e zero deps.

Referência: [VueUse — refDebounced](https://vueuse.org/shared/refDebounced/) · [Vue — Composables](https://vuejs.org/guide/reusability/composables.html)


---

# 8. Performance de JavaScript

**Impacto da seção:** LOW-MEDIUM

## 8.1 toSorted()/toReversed() em vez de sort() sobre dado reativo

**Impacto: MEDIUM (não muta o cache do Vue Query nem dispara loop reativo)**

`.sort()` muta o array original. Dentro de um `computed` sobre `data.value` do Vue Query, isso **altera o cache** compartilhado com outras telas e pode causar loop de reatividade.

**Incorreto:**

```ts
const ordenados = computed(() =>
  (data.value?.items ?? []).sort((a, b) => a.dataAgendamento.localeCompare(b.dataAgendamento)),
)
```

**Correto (ES2023 — suportado pelo target do Vite 7):**

```ts
const ordenados = computed(() =>
  (data.value?.items ?? []).toSorted((a, b) => a.dataAgendamento.localeCompare(b.dataAgendamento)),
)
```

Se a ordenação é da listagem paginada, ela pertence ao **backend** (`OrderBy` no QueryDto) — ordenar client-side só a página atual é bug de UX.

Referência: [MDN — Array.prototype.toSorted](https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/Array/toSorted)

## 8.2 Early return em handlers e computeds

**Impacto: LOW-MEDIUM (menos aninhamento, menos trabalho desnecessário)**

Valide e saia cedo. O estilo novo do repo já usa `if (...) { return; }` — mantenha.

**Incorreto:**

```ts
async function onSubmit() {
  const valid = await formRef.value?.validate()
  if (valid?.valid) {
    if (!isEdit.value || form.password) {
      // ... 30 linhas
      emit('submit', payload)
    }
  }
}
```

**Correto:**

```ts
async function onSubmit() {
  const result = await formRef.value?.validate()
  if (!result?.valid) { return; }
  if (isEdit.value && !form.password) { delete payload.password }
  emit('submit', payload)
}
```

Referência: [Vue Style Guide](https://vuejs.org/style-guide/)

## 8.3 Set/Map para lookups repetidos

**Impacto: LOW-MEDIUM (O(n) para O(1) em filtros client-side)**

Em filtros client-side de listas pequenas (fornecedor ↔ produtos vinculados, seleção em `ProdutoSelectModal`), `array.includes`/`find` dentro de outro loop vira O(n·m).

**Incorreto:**

```ts
const disponiveis = computed(() =>
  produtos.value.filter((p) => !fornecedor.value?.produtos?.some((fp) => fp.id === p.id)),
)
```

**Correto:**

```ts
const vinculados = computed(() => new Set(fornecedor.value?.produtos?.map((p) => p.id)))
const disponiveis = computed(() => produtos.value.filter((p) => !vinculados.value.has(p.id)))

// lookup por id para montar labels
const produtoPorId = computed(() => new Map(produtos.value.map((p) => [p.id, p])))
const nome = produtoPorId.value.get(item.produtoId)?.nome ?? '—'
```

Referência: [MDN — Set](https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/Set)
