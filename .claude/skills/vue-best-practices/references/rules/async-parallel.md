---
title: Promise.all para operações independentes
impact: CRITICAL
impactDescription: 2-5x mais rápido em telas que carregam várias fontes
tags: async, parallelization, promises, waterfalls
---

## Promise.all para operações independentes

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
