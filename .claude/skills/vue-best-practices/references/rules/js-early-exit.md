---
title: Early return em handlers e computeds
impact: LOW-MEDIUM
impactDescription: menos aninhamento, menos trabalho desnecessário
tags: javascript, early-return, readability
---

## Early return em handlers e computeds

**Impacto: LOW-MEDIUM (menos aninhamento, menos trabalho desnecessário)**

Valide e saia cedo. O estilo novo do repo já usa `if (...) { return; }` — mantenha.

**Incorreto:**

```ts
async function onSubmit() {
  const valid = await formRef.value?.validate()
  if (valid?.valid) {
    if (!isEdit.value || form.password) {
      // ... 30 linhas
      emit('submit', payload)
    }
  }
}
```

**Correto:**

```ts
async function onSubmit() {
  const result = await formRef.value?.validate()
  if (!result?.valid) { return; }
  if (isEdit.value && !form.password) { delete payload.password }
  emit('submit', payload)
}
```

Referência: [Vue Style Guide](https://vuejs.org/style-guide/)
