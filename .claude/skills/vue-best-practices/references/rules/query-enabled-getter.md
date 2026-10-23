---
title: enabled reativo (getter), nunca booleano estático
impact: HIGH
impactDescription: detalhe carrega quando o id chega; evita fetch com id vazio
tags: vue-query, enabled, dependent-queries
---

## enabled reativo (getter), nunca booleano estático

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
