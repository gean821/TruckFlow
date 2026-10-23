---
title: defineProps e defineEmits tipados (tupla), nunca array de strings nem any
impact: HIGH
impactDescription: contrato quebrado vira erro no vue-tsc (CI), não bug em produção
tags: component, typescript, defineEmits, defineProps
---

## defineProps e defineEmits tipados (tupla), nunca array de strings nem any

**Impacto: HIGH (contrato quebrado vira erro no vue-tsc — que roda no CI —, não bug em produção)**

O CI roda `vue-tsc --build`. Isso só protege o que está tipado. `defineEmits(["close", "submit"])` e `initialData?: any` desligam a checagem justamente no contrato form ↔ view.

**Incorreto (`FornecedorFormModal.vue`, `ProdutoFormModal.vue`, `LocalDescargaFormModal.vue` — não copie):**

```ts
const props = defineProps<{ open: boolean; initialData?: any }>()
const emit = defineEmits(["close", "submit"])
const formRef = ref<any>(null)
```

**Correto (`UsuarioFormModal.vue`, com o `formRef` tipado):**

```ts
import type { VForm } from 'vuetify/components'

const props = defineProps<{
  open: boolean
  initialData?: FornecedorResponse
  loading?: boolean
}>()

const emit = defineEmits<{
  close: []
  submit: [payload: FornecedorCreateDto | FornecedorUpdateDto]
}>()

const formRef = ref<VForm | null>(null)

async function onSubmit() {
  const result = await formRef.value?.validate()
  if (!result?.valid) return
  emit('submit', { ...form, cnpj: form.cnpj.replace(/\D/g, '') })
}
```

Defaults: `withDefaults(defineProps<Props>(), { loading: false })` ou, em 3.5, destructure com default: `const { loading = false } = defineProps<Props>()`.

Referência: [Vue — Typing Component Props](https://vuejs.org/guide/typescript/composition-api.html#typing-component-props) · [Typing Component Emits](https://vuejs.org/guide/typescript/composition-api.html#typing-component-emits)
