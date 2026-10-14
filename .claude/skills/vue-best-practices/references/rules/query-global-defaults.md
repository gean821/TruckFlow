---
title: QueryClient com defaults globais e QueryCache.onError
impact: HIGH
impactDescription: erro de query nunca mais fica silencioso; retry sem martelar 4xx
tags: vue-query, query-client, error-handling, retry
---

## QueryClient com defaults globais e QueryCache.onError

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
