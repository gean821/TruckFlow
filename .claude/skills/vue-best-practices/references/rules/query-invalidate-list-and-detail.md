---
title: Mutations invalidam lista E detalhe (e entidades afetadas)
impact: CRITICAL
impactDescription: evita detalhe/dashboard desatualizado após editar ou fazer check-in
tags: vue-query, mutation, invalidation
---

## Mutations invalidam lista E detalhe (e entidades afetadas)

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
