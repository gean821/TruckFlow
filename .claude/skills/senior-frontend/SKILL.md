---
name: senior-frontend
description: Toolkit de frontend sênior para o TruckFlowApp (Vue 3.5 + TypeScript + TanStack Vue Query + Vuetify 3 + Vite 7 + Pinia). Inclui scaffolder da cadeia por entidade (types → Service → queries → hook → FormModal → View), auditor de padrões do repo e analisador de bundle, além de referências de padrões, segurança, testes e otimização Vite. Use ao criar feature/tela nova, revisar PR do front, padronizar módulo legado ou investigar bundle.
---

# Senior Frontend — TruckFlowApp

Toolkit para desenvolver e revisar o admin web (`C:\ESTUDO\TruckFlowApp`) no padrão do projeto.

## Quick start

```bash
# 1. Gerar a cadeia completa de uma entidade nova (nunca sobrescreve)
python scripts/feature_scaffolder.py Transportadora --paged --dry-run
python scripts/feature_scaffolder.py Transportadora --paged --repo C:/ESTUDO/TruckFlowApp

# 2. Auditar padrões (repo todo ou só os arquivos do PR)
python scripts/pattern_auditor.py --repo C:/ESTUDO/TruckFlowApp --no-info
python scripts/pattern_auditor.py --files src/views/TransportadoraView.vue src/hooks/useTransportadora.ts

# 3. Analisar bundle
python scripts/bundle_analyzer.py --build          # roda pnpm build-only e mede dist/assets
python scripts/bundle_analyzer.py --source-only    # só anti-padrões de import
```

Todos os scripts têm `--help`. Use como caixa-preta; leia o código só se precisar customizar.

## O padrão (inegociável em código novo)

```
src/entities/<x>.types.ts          ← tipos espelhando DTOs do backend (ResponseDto, CreateDto, UpdateDto, ListQueryDto)
src/services/<X>Service.ts         ← factory `export const XService = () => ({...})`, só axios via `http`
src/queries/<x>.queries.ts         ← key factory + useXxxQuery (MaybeRef params, keepPreviousData, enabled getter)
src/hooks/use<X>.ts                ← mutations (create/update/remove/setStatus) + invalidação + toast
src/components/Forms/<X>FormModal.vue  ← form "burro": v-dialog + v-form + rules, emite `submit` tipado
src/views/<X>View.vue              ← página: filtros em route.query, debounce ~350ms, paginação, modal, ConfirmDialog
```

Regras:
1. **View/componente nunca importa `XService` nem `http`.** Consome `useXxxQuery` (leitura) e `useX()` (escrita).
2. **Pinia só para UI/sessão** (`AuthStore`, `ToastStore`, `ComunicacaoDialogStore`). Dado de servidor é Vue Query.
3. **Listagem de tela é paginada server-side** (`PaginatedResponse<T>`: `items`, `pageNumber`, `pageSize`, `totalCount`, `totalPages`).
4. **Tipado de ponta a ponta**: `defineProps<{...}>()`, `defineEmits<{ submit: [payload: X] }>()`, `ref<VForm | null>`, zero `any`.
5. **Erro do backend chega ao usuário**: toast com `error.response.data.message` e fallback em pt-BR.
6. **Content-Type já é global** em `src/http/http.ts` — não passe header manual em `http.*` (exceto upload multipart, que sobrescreve por request).
7. **Edits cirúrgicos** em arquivo legado: corrija o pedido, não reescreva o módulo inteiro para o padrão novo sem pedido.

Referências de código real (copie destes):

| Peça | Arquivo de referência |
|---|---|
| Cadeia paginada completa | `entities/usuario.types.ts` → `services/UsuarioService.ts` → `queries/usuario.queries.ts` → `hooks/useUsuario.ts` → `components/Forms/UsuarioFormModal.vue` → `views/ManageUserView.vue` |
| Invalidação multi-key | `hooks/usePlanejamento.ts` |
| HTTP + refresh single-flight | `http/http.ts` |
| Auth/roles/guard | `stores/AuthStore.ts`, `shared/auth/roles.ts`, `hooks/usePermissions.ts`, `router/index.ts` |
| SSE | `composables/useRealtimeNotifications.ts` |

**Não copie** (smells conhecidos, o auditor aponta): fetch manual em `UpcomingAppointments.vue` e `RecebimentosOrfaosView.vue`; service direto em `GradeView.vue`, `VisualizarAgendamentoView.vue`, `MinhaContaView.vue`; `useMutation` dentro de `EsqueciSenhaDialog.vue`; `enabled: !!id` estático em fornecedor/produto/grade/agendamento/...; `defineEmits([...])` sem tipo nos FormModals antigos; `toast.notify(..., 3)` em `LoginView.vue` (duração é ms); `v-maska` sem o pacote `maska` instalado.

## Scripts

### `feature_scaffolder.py`
Gera os 6 arquivos acima a partir dos templates do padrão Usuario. Flags: `--paged`, `--plural`, `--endpoint`, `--label`, `--plural-label`, `--repo`, `--dry-run [--show]`. Pula arquivos existentes. Saída validada com `vue-tsc` contra o repo real (zero erros) e com o `pattern_auditor` (zero violações). Depois de gerar: ajuste os tipos ao DTO real, registre a rota lazy com `meta.roles` via `RoleGroups`, adicione no Navbar, rode `pnpm build`.

### `pattern_auditor.py`
Regras: `SVC-IN-UI`, `HTTP-OUTSIDE`, `MUTATION-IN-UI`, `TOAST-MS` (error); `STATIC-ENABLED`, `ANY`, `UNTYPED-EMITS`, `NO-KEEPPREV` (warn); `NO-SIGNAL` (info). `--only`, `--files`, `--json`, `--no-info`. Exit 1 se houver error — serve para pre-commit. Em PR, audite só os arquivos tocados (`--files`); o legado tem dívida conhecida.

### `bundle_analyzer.py`
Lista chunks de `dist/assets` (bruto + gzip estimado), sinaliza JS > `--limit` KB (250), e checa no código: Vuetify importado inteiro, ApexCharts global, `@mdi/font`, views importadas estaticamente no router.

## Referências

- `references/vue_patterns.md` — a cadeia por entidade completa (exemplo Transportadora), key factory, formulários, listagem com URL state, permissões.
- `references/frontend_best_practices.md` — segurança (token em memória, refresh, XSS, CSP), envelope de erro, testes (Vitest + Vue Test Utils + Playwright), acessibilidade, checklist de PR.
- `references/vite_optimization_guide.md` — passo a passo de bundle: medir, tree-shaking do Vuetify, chunks, ApexCharts lazy, ícones, cache na Vercel.
- Skills irmãs: `vue-best-practices` (39 regras), `vue-router-vite-best-practices`, `frontend-design`, `webapp-testing`.

## Stack

Vue 3.5 · TypeScript 5.8 · Vite 7 · Vuetify 3.10 + MDI · TanStack Vue Query 5.92 · Pinia 3 · Vue Router 4 · Axios 1.x · date-fns 4 · ApexCharts · `@microsoft/fetch-event-source` (SSE) · pnpm · Vercel. Testes disponíveis (ainda não usados): Vitest 3, @vue/test-utils 2, jsdom, Playwright.

## Comandos

```bash
pnpm dev          # Vite :5173
pnpm build        # vue-tsc --build + vite build (é o gate do CI)
pnpm type-check   # só vue-tsc
pnpm preview
```

## Fluxo de trabalho de uma feature

1. Contrato primeiro: leia o DTO/controller no backend (`C:\ESTUDO\TruckFlow`) e espelhe em `entities/<x>.types.ts`.
2. Scaffold (`feature_scaffolder.py`) ou edite a cadeia existente.
3. UI com `frontend-design` (tema, estados de loading/vazio/erro, a11y).
4. `pnpm build` + `pattern_auditor.py --files ...`.
5. Teste no browser com `webapp-testing` — incluindo **usuário de outra empresa** (multi-tenant).
