---
title: Update otimista com rollback para toggles de status
impact: MEDIUM
impactDescription: switch ativo/inativo responde instantâneo; volta se o servidor recusar
tags: vue-query, optimistic-update, mutation
---

## Update otimista com rollback para toggles de status

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
