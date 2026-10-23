# Guia de otimização Vite — TruckFlowApp

Passo a passo para medir e reduzir o bundle do admin. Público real: PC antigo da portaria e tablet no pátio, às vezes em 4G — o tempo até o login ficar utilizável importa.

> Regra zero: **meça antes e depois**. Nenhuma otimização entra sem número.

---

## 1. Medir

```bash
cd C:/ESTUDO/TruckFlowApp
pnpm build                                   # vue-tsc + vite build
python <skill>/scripts/bundle_analyzer.py    # tabela de chunks + anti-padrões
```

Treemap detalhado (opcional, dev-only):

```bash
pnpm add -D rollup-plugin-visualizer
```

```ts
// vite.config.ts
import { visualizer } from "rollup-plugin-visualizer";
plugins: [
  vue(),
  process.env.ANALYZE && visualizer({ filename: "stats.html", gzipSize: true, brotliSize: true, open: true }),
].filter(Boolean),
```

```bash
ANALYZE=1 pnpm build-only
```

Registre no PR: tamanho do chunk de entrada (`index-*.js`) bruto e gzip, antes/depois.

Runtime: Lighthouse (Chrome DevTools) em modo "Mobile, Slow 4G" na tela de login e no dashboard. Alvos: LCP < 2.5s, INP < 200ms, CLS < 0.1.

---

## 2. Vuetify com tree-shaking (maior ganho esperado)

Hoje: `import * as components from 'vuetify/components'` + `directives` em `main.ts` → Vuetify inteiro no chunk de entrada.

```bash
pnpm add -D vite-plugin-vuetify
```

```ts
// vite.config.ts
import vuetify from "vite-plugin-vuetify";
plugins: [vue(), vuetify({ autoImport: true }), vueDevTools()],
```

```ts
// src/plugins/vuetify.ts
import "vuetify/styles";
import "@mdi/font/css/materialdesignicons.css";
import { createVuetify } from "vuetify";
import { truckflowTheme } from "@/theme/truckflow";

export default createVuetify({
  theme: { defaultTheme: "truckflow", themes: { truckflow: truckflowTheme } },
});
```

Cuidados:
- Diretivas usadas no template (`v-ripple`, `v-intersect`, `v-click-outside`) são auto-importadas; usadas via `resolveDirective` não.
- `v-maska` não é do Vuetify (e o pacote nem está instalado) — não confunda o warning.
- Teste visual rápido de todas as telas depois (componentes com `h()` precisam de import manual).

---

## 3. ApexCharts sob demanda

Hoje `app.use(VueApexCharts)` em `main.ts` → ~500KB no bundle inicial, inclusive no login.

```ts
// remover de main.ts:  app.use(VueApexCharts)

// components/dashboard/StatusChart.vue e OccupancyChart.vue
import { defineAsyncComponent } from "vue";
const ApexChart = defineAsyncComponent(() => import("vue3-apexcharts"));
```

```vue
<ApexChart type="donut" :options="options" :series="series" height="280" />
```

O chunk do Apex passa a ser baixado só quando o dashboard renderiza.

---

## 4. Rotas e componentes pesados

- Todas as rotas autenticadas já são lazy — mantenha (`() => import(...)`).
- Modais grandes e raros (`RelatorioDialog`, `AgendamentoAvulsoModal`, `AuditDrawer`) com `defineAsyncComponent` + `v-if`.
- Prefetch de rota provável: no `mouseenter` do item do Navbar, `import('@/views/XView.vue')` (o browser cacheia o módulo).

---

## 5. Chunks manuais (vendor estável)

Vendors mudam menos que o código do app; separados, ficam em cache entre deploys.

```ts
build: {
  rollupOptions: {
    output: {
      manualChunks: {
        vue: ["vue", "vue-router", "pinia"],
        vuetify: ["vuetify"],
        query: ["@tanstack/vue-query", "axios"],
        charts: ["apexcharts", "vue3-apexcharts"],
        date: ["date-fns"],
      },
    },
  },
  chunkSizeWarningLimit: 600,
},
```

Não exagere: chunk demais = request demais. 4-6 vendors é o ponto doce.

`date-fns` v4 é tree-shakeable — importe funções nomeadas (`import { format } from "date-fns"`) e o locale específico (`import { ptBR } from "date-fns/locale"`), nunca o pacote inteiro.

---

## 6. Ícones e fontes

- `@mdi/font` = fonte com ~7.000 ícones (~300KB woff2 + CSS). Alternativa `@mdi/js` + iconset `mdi-svg` só inclui os usados, mas exige trocar `prepend-icon="mdi-x"` por binding em todo o projeto — decisão de projeto, faça por tela.
- Fontes: Montserrat vem do Google Fonts (`index.html`). Adicione `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>` e `&display=swap`. `@fontsource/roboto` está instalado e não é usado — remover do `package.json` é seguro (confirme com `grep -r fontsource src`).

---

## 7. Cache na Vercel

- `/assets/*` (nome com hash): `Cache-Control: public, max-age=31536000, immutable`.
- `index.html`: `no-cache` (senão o usuário fica preso numa versão antiga depois do deploy).

```json
{
  "headers": [
    { "source": "/assets/(.*)", "headers": [{ "key": "Cache-Control", "value": "public, max-age=31536000, immutable" }] },
    { "source": "/index.html", "headers": [{ "key": "Cache-Control", "value": "no-cache" }] }
  ],
  "rewrites": [{ "source": "/(.*)", "destination": "/index.html" }]
}
```

Deploy novo com usuário com a aba aberta: o lazy import de um chunk antigo pode dar 404. Trate em `router.onError` recarregando a página uma vez:

```ts
router.onError((err, to) => {
  if (/Failed to fetch dynamically imported module|Importing a module script failed/.test(String(err?.message))) {
    if (!sessionStorage.getItem("chunk-reload")) {
      sessionStorage.setItem("chunk-reload", "1");
      window.location.assign(to.fullPath);
    }
  }
});
router.afterEach(() => sessionStorage.removeItem("chunk-reload"));
```

---

## 8. Dev server

- `pnpm dev` (porta 5173 — é a origem liberada no CORS da API).
- `vite-plugin-vue-devtools` só em dev (já é o comportamento padrão do plugin).
- Se o HMR ficar lento: `server.warmup.clientFiles: ["./src/main.ts", "./src/views/DashboardView.vue"]`.

---

## 9. Checklist de otimização

- [ ] Medido antes (chunk de entrada bruto/gzip, Lighthouse login)
- [ ] Vuetify via `vite-plugin-vuetify`
- [ ] ApexCharts e modais raros lazy
- [ ] Rotas lazy (nenhum import estático de view no router, exceto Login/Home)
- [ ] `manualChunks` para vendors
- [ ] Headers de cache na Vercel + tratamento de chunk 404 pós-deploy
- [ ] Medido depois, números no PR

Fontes: [Vite — Building for Production](https://vite.dev/guide/build) · [Vite — Performance](https://vite.dev/guide/performance) · [Vuetify — Treeshaking](https://vuetifyjs.com/en/features/treeshaking/) · [Vue — Performance](https://vuejs.org/guide/best-practices/performance.html) · [web.dev — Core Web Vitals](https://web.dev/articles/vitals) · [Vercel — Caching headers](https://vercel.com/docs/headers/cache-control-headers)
