---
title: staleTime por tipo de recurso
impact: MEDIUM-HIGH
impactDescription: corta refetch desnecessário de dados quase estáticos
tags: vue-query, stale-time, cache
---

## staleTime por tipo de recurso

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
