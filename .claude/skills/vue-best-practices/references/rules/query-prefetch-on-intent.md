---
title: Prefetch do detalhe na intenção (hover/foco)
impact: MEDIUM
impactDescription: modal/drawer de detalhe abre com dado já em cache
tags: vue-query, prefetch, ux
---

## Prefetch do detalhe na intenção (hover/foco)

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
