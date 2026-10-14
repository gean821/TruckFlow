---
title: Vuetify com tree-shaking (vite-plugin-vuetify), não import * as components
impact: CRITICAL
impactDescription: corta centenas de KB do bundle inicial
tags: bundle, vuetify, tree-shaking, vite
---

## Vuetify com tree-shaking (vite-plugin-vuetify), não import * as components

**Impacto: CRITICAL (corta centenas de KB do bundle inicial)**

Registrar **todos** os componentes e diretivas do Vuetify coloca a biblioteca inteira no bundle, mesmo o que nunca é usado. O `vite-plugin-vuetify` com `autoImport` importa só o que aparece nos templates.

**Incorreto (`src/main.ts` hoje, não copie):**

```ts
import * as components from 'vuetify/components'
import * as directives from 'vuetify/directives'
const vuetify = createVuetify({ components, directives })
```

**Correto:**

```ts
// vite.config.ts
import vuetify from 'vite-plugin-vuetify'
export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true }), vueDevTools()],
})

// src/plugins/vuetify.ts
import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { truckflowTheme } from '@/theme/truckflow'
export default createVuetify({
  theme: { defaultTheme: 'truckflow', themes: { truckflow: truckflowTheme } },
})
```

Depois da troca, rode `pnpm build` e compare o tamanho do chunk `index-*.js` (script `bundle_analyzer.py` da skill `senior-frontend`). Componentes usados só via `h()`/render function precisam de import explícito.

Referência: [Vuetify — Treeshaking](https://vuetifyjs.com/en/features/treeshaking/) · [vite-plugin-vuetify](https://github.com/vuetifyjs/vuetify-loader/tree/master/packages/vite-plugin)
