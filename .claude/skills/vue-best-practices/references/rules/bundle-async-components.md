---
title: defineAsyncComponent para dialogs e gráficos pesados
impact: HIGH
impactDescription: ApexCharts (~500KB) e modais raros saem do chunk da tela
tags: bundle, defineAsyncComponent, apexcharts, dialogs
---

## defineAsyncComponent para dialogs e gráficos pesados

**Impacto: HIGH (ApexCharts e modais raros saem do chunk da tela)**

`app.use(VueApexCharts)` em `main.ts` coloca o ApexCharts no bundle **inicial**, inclusive na tela de login. Modais grandes que abrem raramente (`RelatorioDialog`, `AgendamentoAvulsoModal`, `AuditDrawer`) também não precisam vir no chunk da view.

**Incorreto:**

```ts
// main.ts
import VueApexCharts from 'vue3-apexcharts'
app.use(VueApexCharts)
```

**Correto (gráfico carregado só no dashboard):**

```ts
// components/dashboard/StatusChart.vue
import { defineAsyncComponent } from 'vue'
const ApexChart = defineAsyncComponent(() => import('vue3-apexcharts'))
```

```vue
<ApexChart type="donut" :options="options" :series="series" height="280" />
```

**Correto (modal raro):**

```ts
const RelatorioDialog = defineAsyncComponent(() => import('@/components/modals/RelatorioDialog.vue'))
```

```vue
<RelatorioDialog v-if="relatorioAberto" v-model="relatorioAberto" />
```

O `v-if` garante que o chunk só é baixado quando o usuário abre o modal.

Referência: [Vue — Async Components](https://vuejs.org/guide/components/async.html)
