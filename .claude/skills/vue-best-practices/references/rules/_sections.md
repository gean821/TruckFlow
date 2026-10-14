# Seções

Este arquivo define as seções, a ordem, o nível de impacto e a descrição de cada uma.
O ID da seção (entre parênteses) é o prefixo do nome de arquivo usado para agrupar as regras.

Contexto: TruckFlowApp é uma **SPA** (Vue 3.5 + Vite 7 + Vuetify 3 + TanStack Vue Query 5, deploy Vercel). Não há SSR, RSC nem hidratação — regras desse tipo do guia React original foram descartadas.

---

## 1. Estado de servidor com TanStack Vue Query (query)

**Impacto:** CRITICAL
**Descrição:** Quase todo bug de "tela desatualizada", "requisição duplicada" ou "lista que não reage ao filtro" no admin nasce aqui. Query keys reativas, invalidação correta e defaults globais eliminam a maior classe de bugs do front.

## 2. Eliminando waterfalls (async)

**Impacto:** CRITICAL
**Descrição:** Cada `await` sequencial soma uma latência de rede inteira. Na rede da fábrica (Wi-Fi industrial, 4G na portaria), isso é perceptível.

## 3. Tamanho de bundle (bundle)

**Impacto:** CRITICAL
**Descrição:** Vuetify importado inteiro + ApexCharts + MDI font no bundle inicial = login lento em máquina velha da portaria. Code splitting por rota e por componente pesado.

## 4. Reatividade (reactivity)

**Impacto:** HIGH
**Descrição:** O sistema de reatividade do Vue é o equivalente aos re-renders do React: derivar errado (watch em vez de computed), perder reatividade ao desestruturar ou tornar profundo o que não precisa custa correção e performance.

## 5. Renderização (rendering)

**Impacto:** MEDIUM
**Descrição:** Listas grandes (agendamentos do dia, auditoria), tabelas e cards. `key` estável, `v-memo`, virtualização e `v-show`/`v-if` corretos.

## 6. API de componentes (component)

**Impacto:** MEDIUM
**Descrição:** Contratos tipados entre componentes (`defineProps`, `defineEmits` em tupla, `defineModel`, `InjectionKey`). Erro de contrato vira erro de compilação no `vue-tsc`, não bug em produção.

## 7. Composables (composable)

**Impacto:** MEDIUM
**Descrição:** Lógica repetida (debounce de busca, sincronização filtro ↔ `route.query`) vira composable único. Cleanup no unmount sempre.

## 8. Performance de JavaScript (js)

**Impacto:** LOW-MEDIUM
**Descrição:** Micro-otimizações para caminhos quentes (filtros client-side de listas pequenas, transformação de dados para gráficos).
