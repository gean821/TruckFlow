# Boas práticas de frontend — TruckFlowApp

Segurança, tratamento de erro, testes, acessibilidade e checklist de PR para o admin web.

---

## 1. Segurança

Contexto: Aurora faz pen-test por terceiro e preenche questionário SAD (AppSec é pilar com peso). O front é superfície de ataque real.

### Sessão
- **Access token só em memória** (Pinia `AuthStore`) — já é assim. Nunca `localStorage`/`sessionStorage` para token.
- **Refresh token em cookie httpOnly** (`Secure`, `SameSite`), enviado por `withCredentials: true` (já configurado em `http.ts`). O JS nunca lê o refresh.
- **Refresh single-flight**: o interceptor de `http.ts` compartilha uma `refreshPromise` entre 401s concorrentes. Não crie outro interceptor de refresh nem chame `/Auth/refresh` fora do `AuthStore`/`http.ts`.
- `restoreSession()` roda no boot (`main.ts`) antes do router — mantenha a ordem.
- Logout: `AuthStore.logout()` (chama `/Auth/logout` para revogar o refresh no servidor) — nunca só limpar estado local.
- SSE: o `fetchEventSource` envia `Authorization` capturado na conexão. Ao trocar o token (refresh), a conexão precisa ser refeita com o novo — reaja a **qualquer** mudança de `auth.token`, não só null↔valor.

### XSS
- **Nunca `v-html` com dado vindo da API** (nome de fornecedor, observação de agendamento, corpo de notificação, mensagem do motorista). Interpolação `{{ }}` já escapa.
- Se precisar renderizar markdown leve (corpo de `Notificacao`, ADR-0002), sanitize com DOMPurify num componente único `SafeMarkdown.vue`.
- Links montados com dado do usuário (deep-link WhatsApp `wa.me/55{telefone}?text=...`): `encodeURIComponent` em todo trecho e `rel="noopener noreferrer"` com `target="_blank"`.
- Redirect pós-login (`?redirect=`): aceite só paths internos (`startsWith('/') && !startsWith('//')`).

### CSP e headers (Vercel)
Defina em `vercel.json` → `headers`:
```json
{
  "source": "/(.*)",
  "headers": [
    { "key": "Content-Security-Policy", "value": "default-src 'self'; connect-src 'self' https://<api-host>; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com data:; frame-ancestors 'none'; base-uri 'self'" },
    { "key": "X-Content-Type-Options", "value": "nosniff" },
    { "key": "Referrer-Policy", "value": "strict-origin-when-cross-origin" },
    { "key": "Permissions-Policy", "value": "camera=(), microphone=(), geolocation=()" }
  ]
}
```
`style-src 'unsafe-inline'` é exigido pelo Vuetify (estilos inline de tema). Valide no Preview da Vercel antes de produção — CSP errada quebra login SSO (redirect para a API).

### Variáveis de ambiente
Tudo `VITE_*` é público no bundle. Nenhuma chave, segredo ou URL interna sensível no front.

### Multi-tenant
O front **não** filtra por empresa. Nunca envie `empresaId` como filtro de segurança; o backend usa o claim do JWT. No teste manual/E2E, sempre logue com usuário de **outra empresa** e confirme listagens vazias e 404 em detalhe.

---

## 2. Tratamento de erro

### Envelope
Confira o formato real no middleware de exceções do backend (`TruckFlow/Middlewares`). O front deve aceitar tanto o envelope de negócio (`{ message }`) quanto `ValidationProblemDetails` do ASP.NET (`{ title, errors: { Campo: ["msg"] } }`):

```ts
// src/http/errors.ts
import { isAxiosError } from "axios";

type ApiErrorBody = { message?: string; title?: string; detail?: string; errors?: Record<string, string[]> };

export function getErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError<ApiErrorBody>(error)) { return fallback; }
  if (!error.response) { return "Sem conexão com o servidor. Verifique a internet e tente novamente."; }
  const body = error.response.data;
  if (body?.errors) { return Object.values(body.errors).flat()[0] ?? fallback; }
  return body?.message ?? body?.detail ?? body?.title ?? fallback;
}

export function getStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined;
}
```

### Onde mostrar
| Origem | Onde |
|---|---|
| Mutation | toast no `onError` do hook (mensagem do backend + fallback) |
| Query de tela | `v-alert` com "Tentar novamente" na view (`isError` + `refetch`) |
| Query em background (refetch falhou com dado em tela) | `QueryCache.onError` global → toast discreto |
| 401 | interceptor (refresh → retry → logout) — nunca trate na view |
| 403 | página/estado "sem permissão" — não redirecione para login |
| 409 (concorrência: slot já reservado, `xmin` mudou) | toast com a mensagem + invalidar a query para mostrar o estado atual |

### Não faça
- `catch (e: any) { console.error(e) }` sem feedback ao usuário.
- `toast.notify(msg, 'error', 3)` — duração é **milissegundos** (3 = some instantaneamente; bug atual do `LoginView`).
- Engolir erro em `finally` com redirect incondicional (bug real já corrigido no SSO: `finally` roda mesmo após `return` no `try`).

---

## 3. Testes

Dependências já instaladas (Vitest 3, @vue/test-utils 2, jsdom, Playwright), mas **não há testes nem script `test`**. Ao introduzir:

```json
// package.json
"test": "vitest",
"test:run": "vitest run",
"test:e2e": "playwright test"
```

```ts
// vitest.config.ts
import { defineConfig, mergeConfig } from "vitest/config";
import viteConfig from "./vite.config";
export default mergeConfig(viteConfig, defineConfig({
  test: { environment: "jsdom", globals: true, server: { deps: { inline: ["vuetify"] } } }
}));
```

### O que testar primeiro (maior risco × menor custo)
1. **Funções puras**: `getErrorMessage`, `hasRole`/`RoleGroups`, `utils/date-format.ts`, `utils/audit.ts`, mapa de status.
2. **Interceptor de `http.ts`**: 401 → um único refresh para N requests concorrentes → retry; refresh falhou → `clearSession` + `/login`. Use `axios-mock-adapter` ou `msw`.
3. **Hooks**: mutation invalida a key certa (renderize com `QueryClient` real e espione `invalidateQueries`).
4. **FormModal**: rules bloqueiam submit inválido; payload emitido limpa máscara.
5. **E2E Playwright** (ver skill `webapp-testing`): login, check-in, listagem paginada com busca, e o **gate multi-tenant** (usuário da Empresa B não vê dado da A).

### Mocks
Mock de HTTP no front é ok para teste unitário de componente. A regra "sem mock, Postgres real" é dos **testes de integração do backend** — não confunda. E2E roda contra a API real local.

Montagem com Vuetify:
```ts
import { mount } from "@vue/test-utils";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components"; // ok em teste
const vuetify = createVuetify({ components });
mount(TransportadoraFormModal, { props: { open: true }, global: { plugins: [vuetify] } });
```

Adicione os testes ao CI (`.github/workflows/ci.yml` hoje só roda `pnpm build`).

---

## 4. Acessibilidade e UX operacional

Ver skill `frontend-design`. Mínimo em todo PR: `aria-label` em botão só-ícone, `label` em todo input, estados de loading/vazio/erro, contraste AA, operável por teclado, alvo de toque ≥ 24px.

---

## 5. Performance

Ver skill `vue-best-practices` e `vite_optimization_guide.md`. Mínimo: rotas lazy, nada de fetch manual, `keepPreviousData` em paginação, `key` estável em `v-for`.

---

## 6. Checklist de PR (front)

- [ ] Cadeia respeitada: view → hook/query → service → http
- [ ] Tipos espelham o DTO do backend (camelCase, opcionais `?: T | null`)
- [ ] Query: key reativa, `enabled` getter, `keepPreviousData` em lista, `signal`
- [ ] Mutation: invalida lista + detalhe + entidades afetadas; toast com erro do backend
- [ ] Listagem paginada server-side, busca debounced, estado em `route.query`
- [ ] Props/emits tipados, zero `any` novo
- [ ] Rota lazy com `meta.roles` via `RoleGroups`; ação escondida via `usePermissions` **e** protegida no backend
- [ ] Sem `v-html` com dado da API; sem segredo em `VITE_*`
- [ ] Loading/vazio/erro; a11y básica; copy pt-BR
- [ ] `pnpm build` passa; `pattern_auditor.py --files <tocados>` sem error
- [ ] Testado com usuário de outra empresa
- [ ] Commit sem linhas de atribuição a IA

Fontes: [OWASP — Cross Site Scripting Prevention](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html) · [OWASP — JWT Cheat Sheet (armazenamento de token)](https://cheatsheetseries.owasp.org/cheatsheets/JSON_Web_Token_for_Java_Cheat_Sheet.html) · [MDN — CSP](https://developer.mozilla.org/docs/Web/HTTP/CSP) · [Vue — Security](https://vuejs.org/guide/best-practices/security.html) · [Vitest](https://vitest.dev/) · [Vue Test Utils](https://test-utils.vuejs.org/) · [Playwright](https://playwright.dev/) · [Testing Library — Guiding Principles](https://testing-library.com/docs/guiding-principles)
