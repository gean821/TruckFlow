---
title: Ícones MDI — font inteira vs @mdi/js por ícone
impact: MEDIUM
impactDescription: ~300KB de font/CSS a menos quando migrado para SVG
tags: bundle, icons, mdi, vuetify
---

## Ícones MDI — font inteira vs @mdi/js por ícone

**Impacto: MEDIUM (~300KB de font/CSS a menos quando migrado para SVG)**

`@mdi/font` baixa a fonte com os ~7.000 ícones para usar dezenas. Com `@mdi/js` + iconset `mdi-svg`, só os ícones importados entram no bundle. É uma migração grande (todo `prepend-icon="mdi-..."` vira binding) — faça por tela, não num PR gigante, e só se o bundle for um problema medido.

**Atual (aceitável, mas pesado):**

```vue
<v-btn prepend-icon="mdi-truck-check">Check-in</v-btn>
```

**Alvo:**

```ts
// src/plugins/vuetify.ts
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'
createVuetify({ icons: { defaultSet: 'mdi', aliases, sets: { mdi } } })
```

```vue
<script setup lang="ts">
import { mdiTruckCheck } from '@mdi/js'
</script>
<template>
  <v-btn :prepend-icon="mdiTruckCheck">Check-in</v-btn>
</template>
```

Não misture os dois sistemas na mesma tela. Decida no nível do projeto.

Referência: [Vuetify — Icon Fonts](https://vuetifyjs.com/en/features/icon-fonts/)
