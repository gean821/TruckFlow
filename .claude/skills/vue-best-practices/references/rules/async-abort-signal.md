---
title: Repasse o AbortSignal do queryFn para o axios
impact: MEDIUM-HIGH
impactDescription: busca com debounce cancela a requisição anterior de verdade
tags: async, abort, axios, vue-query, search
---

## Repasse o AbortSignal do queryFn para o axios

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
