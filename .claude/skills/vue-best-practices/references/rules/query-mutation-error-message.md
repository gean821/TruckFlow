---
title: Mensagem de erro do backend no toast (com fallback)
impact: HIGH
impactDescription: usuário vê "Slot já reservado" em vez de "Erro ao salvar"
tags: vue-query, mutation, error-handling, toast
---

## Mensagem de erro do backend no toast (com fallback)

**Impacto: HIGH (usuário vê "Slot já reservado" em vez de "Erro ao salvar")**

O backend devolve mensagens de negócio em PT-BR (`BusinessException`, `NotFoundException`). Alguns hooks já leem `error?.response?.data?.message` (useAgendamento, useGrade); outros jogam fora e mostram texto fixo. Centralize a extração num helper tipado.

**Incorreto (`src/hooks/useFornecedor.ts`, não copie):**

```ts
onError: () => {
  toast.notify("Erro ao cadastrar", "error")
}
```

**Incorreto 2 (`any` espalhado):**

```ts
onError: (error: any) => toast.notify(error?.response?.data?.message ?? "Erro", "error")
```

**Correto:**

```ts
// src/http/errors.ts
import { isAxiosError } from 'axios'

type ApiErrorBody = { message?: string; title?: string; detail?: string; errors?: Record<string, string[]> }

export function getErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError<ApiErrorBody>(error)) return fallback
  const body = error.response?.data
  if (body?.errors) return Object.values(body.errors).flat()[0] ?? fallback // ModelState/ValidationProblem
  return body?.message ?? body?.detail ?? body?.title ?? fallback
}

// hook
onError: (error) => toast.notify(getErrorMessage(error, 'Erro ao cadastrar fornecedor.'), 'error')
```

Confira o shape real do envelope de erro no middleware de exceções do backend antes de ajustar `ApiErrorBody`.

Referência: [Mutations](https://tanstack.com/query/latest/docs/framework/vue/guides/mutations) · [axios isAxiosError](https://axios-http.com/docs/handling_errors)
