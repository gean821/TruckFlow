---
name: frontend-design
description: Design de interface para o admin TruckFlowApp (Vue 3 + Vuetify 3) — produto B2B operacional usado por portaria, monitor e gerente nas fábricas da Aurora Alimentos. Tema Vuetify TruckFlow (primary #0c2b52), design tokens em vez de hex solto, cores semânticas de StatusAgendamento, densidade operacional, estados de loading/vazio/erro, acessibilidade WCAG 2.2 AA, copy em pt-BR e dashboards com ApexCharts. Use ao criar ou redesenhar telas, componentes, dashboards, cards, tabelas ou dialogs do admin.
license: Complete terms in LICENSE.txt
---

Esta skill guia a criação de interfaces do TruckFlowApp com qualidade de produção. O produto **não é uma landing page**: é ferramenta de trabalho de pátio e doca, usada o dia inteiro, em monitor velho da portaria, notebook do monitor e tablet no pátio. O "estilo" que buscamos é **utilitário industrial com acabamento**: denso, legível, rápido e sem ruído — o oposto do "AI slop" genérico (gradiente roxo, cards fofos espaçados, ícone decorativo em tudo).

## Pensamento de design (antes de codar)

- **Quem usa e onde**: Portaria (check-in rápido, muitas vezes de pé, sol batendo na tela), Monitor (acompanha fila/doca, tela aberta o turno todo), Gerente/Admin (cadastros, relatórios, auditoria). Motorista **não** usa o web — é o app `C:\tf-mobile`.
- **Tarefa principal da tela**: qual é a ação nº 1? Ela tem que estar visível sem scroll e alcançável por teclado.
- **Densidade**: operação > vitrine. Prefira `density="comfortable"` ou `"compact"` em tabelas e listas; reserve espaço generoso só para dashboards e telas de entrada.
- **Estado do mundo**: status do agendamento, horário e placa são a informação que mais importa — hierarquia tipográfica gira em torno disso.

**CRÍTICO**: consistência vence criatividade aqui. Uma tela nova deve parecer da mesma família que `ManageUserView.vue` e `VisualizarAgendamentoView.vue`. Ousadia vai em **clareza** (hierarquia, contraste, ritmo), não em efeito.

## Tema e tokens

Hoje o Vuetify roda **sem tema** e há hex solto em `<style scoped>` (`#1976d2`, `#e2e8f0`, `#0c2b52`) e override inline no `LoginView` (`--v-theme-primary:#0c2b52`). Alvo: um tema único, e componente só usa nome de cor.

```ts
// src/theme/truckflow.ts
import type { ThemeDefinition } from 'vuetify'

export const truckflowTheme: ThemeDefinition = {
  dark: false,
  colors: {
    primary: '#0c2b52',      // azul-marinho TruckFlow (já usado no LoginView)
    secondary: '#3a5a80',
    accent: '#f2a900',       // âmbar sinalização — destaque pontual, nunca fundo grande
    background: '#f5f7fa',
    surface: '#ffffff',
    'surface-variant': '#e9eef4',
    error: '#c62828',
    warning: '#b26a00',      // escuro o bastante para texto sobre branco (AA)
    info: '#1565c0',
    success: '#2e7d32',
    // status do agendamento (ver tabela abaixo)
    'status-disponivel': '#607d8b',
    'status-pendente': '#b26a00',
    'status-agendado': '#5e35b1',
    'status-em-andamento': '#1565c0',
    'status-finalizado': '#2e7d32',
    'status-cancelado': '#c62828',
    'status-expirado': '#e65100',
  },
  variables: {
    'border-color': '#0c2b52',
    'medium-emphasis-opacity': 0.7,
  },
}
```

```ts
// src/plugins/vuetify.ts
createVuetify({
  theme: { defaultTheme: 'truckflow', themes: { truckflow: truckflowTheme } },
  defaults: {
    VBtn: { rounded: 'lg', class: 'text-none' },
    VCard: { rounded: 'lg' },
    VTextField: { variant: 'outlined', density: 'comfortable' },
    VSelect: { variant: 'outlined', density: 'comfortable' },
    VAutocomplete: { variant: 'outlined', density: 'comfortable' },
    VDataTable: { density: 'comfortable', hover: true },
  },
})
```

`defaults` globais codificam os idiomas já recorrentes no repo (`rounded="lg"`, `variant="outlined"`, `class="text-none"`) — componente novo não repete esses props.

Em CSS, use as variáveis do tema: `rgb(var(--v-theme-primary))`, `rgba(var(--v-theme-on-surface), var(--v-medium-emphasis-opacity))`. **Nunca hex novo em `<style scoped>`.**

## Status do agendamento — cor + rótulo + ícone

Hoje o mapa vive dentro de `UpcomingAppointments.vue` (com `purple`, `blue`, `orange`). Centralize em `src/utils/statusAgendamento.ts` e use o mesmo mapa em todo chip, card, gráfico e legenda:

| `StatusAgendamento` | Rótulo | Cor do tema | Ícone MDI |
|---|---|---|---|
| `Disponivel` (0) | Disponível | `status-disponivel` | `mdi-calendar-blank-outline` |
| `Pendente` (1) | Pendente | `status-pendente` | `mdi-clock-alert-outline` |
| `Agendado` (2) | Agendado | `status-agendado` | `mdi-calendar-check` |
| `EmAndamento` (3) | Em andamento | `status-em-andamento` | `mdi-truck-fast-outline` |
| `Finalizado` (4) | Finalizado | `status-finalizado` | `mdi-check-circle-outline` |
| `Cancelado` (5) | Cancelado | `status-cancelado` | `mdi-close-circle-outline` |
| `Expirado` (6) | Expirado | `status-expirado` | `mdi-timer-off-outline` |

```vue
<v-chip :color="statusMeta(a.status).color" :prepend-icon="statusMeta(a.status).icon" size="small" variant="tonal" label>
  {{ statusMeta(a.status).label }}
</v-chip>
```

**Cor nunca é a única pista** (daltonismo, tela com reflexo): sempre rótulo + ícone junto.

## Tipografia

- **Montserrat** já é a fonte do `body` e dos headings (`index.html` + `assets/global.css`). Mantenha; não introduza outra família. `@fontsource/roboto` está instalado e não é usado — não use.
- Hierarquia: título da tela `text-h5 font-weight-bold`, subtítulo `text-body-2 text-medium-emphasis`, dado-chave (placa, horário, nº NF) `text-subtitle-1 font-weight-bold` com `font-variant-numeric: tabular-nums` para alinhar números em colunas.
- Placa e chave de NF-e em fonte monoespaçada (`font-family: ui-monospace, monospace`) — facilita conferência visual de caractere a caractere.

## Layout e composição

- Cabeçalho da tela: título + subtítulo à esquerda, ação primária (`v-btn color="primary"`) à direita. Filtros logo abaixo numa faixa (`v-row dense`), busca primeiro.
- Listagens: card grid (padrão `ManageUserView`) quando o item tem foto/identidade; `v-data-table` quando o usuário compara colunas (agendamentos, recebimentos). Paginação sempre embaixo, `:total-visible="7"`.
- Ações secundárias em ícone com `v-tooltip` (padrão do repo, via slot `activator` + `v-bind="props"`), e ação destrutiva sempre com `ConfirmDialog`.
- Responsivo: o admin precisa funcionar em tablet no pátio (≥ 768px). Use o grid do Vuetify (`cols="12" md="6" lg="4"`), não larguras fixas em px.

## Estados obrigatórios em toda tela de dados

| Estado | Padrão |
|---|---|
| Carregando (sem dado) | `<v-skeleton-loader v-if="isLoading && !data" type="card@6" />` com o formato do conteúdo real |
| Recarregando (com dado) | `<v-progress-linear indeterminate v-if="isFetching" />` no topo — nunca apagar o conteúdo |
| Vazio | ícone `mdi-*` grande em `text-medium-emphasis` + frase do que fazer ("Nenhum fornecedor cadastrado. Cadastre o primeiro.") + botão da ação primária |
| Vazio por filtro | diferente do vazio real: "Nenhum resultado para “{busca}”" + botão "Limpar filtros" |
| Erro | `v-alert type="error" variant="tonal"` com a mensagem do backend e botão "Tentar novamente" (`refetch`) |
| Sem permissão | página `/403`, nunca tela em branco |

## Acessibilidade (WCAG 2.2 AA)

- Contraste ≥ 4.5:1 para texto, ≥ 3:1 para ícones/bordas de input. As cores do tema acima foram escolhidas escuras por isso (`warning` `#b26a00`, não `#ffc107`).
- Todo `v-btn` só com ícone tem `aria-label` (o tooltip não substitui).
- Dialogs: `persistent` só quando há formulário sujo; foco vai para o primeiro campo ao abrir e volta para o botão que abriu ao fechar (Vuetify faz se você não quebrar com `v-if` no activator). `Esc` fecha dialogs sem form.
- Formulário: `label` sempre (placeholder não é label), mensagens de erro das `rules` em pt-BR e específicas ("CNPJ deve ter 14 dígitos", não "Campo inválido").
- Alvo de toque ≥ 24×24 CSS px (WCAG 2.5.8); na portaria/tablet prefira `size="large"` nas ações principais.
- Não dependa de hover para informação essencial (tablet não tem hover).

## Copy (pt-BR)

- Voz direta e operacional: "Registrar entrada", "Confirmar check-in", "Cancelar agendamento" — verbo + objeto.
- Toast de sucesso curto ("Fornecedor cadastrado!"); toast de erro com a **mensagem do backend** quando houver.
- Datas `dd/MM/yyyy HH:mm` via `date-fns` com locale `ptBR` (ver `utils/date-format.ts`); números com `toLocaleString('pt-BR')`; peso com unidade ("12.450 kg").
- Nunca expor termos técnicos ao usuário final ("Guid", "403", "payload"). Erro de rede: "Sem conexão com o servidor. Verifique a internet e tente novamente."

## Dashboards (ApexCharts)

- Carregue o ApexCharts sob demanda (`defineAsyncComponent`), não no `main.ts`.
- Cores das séries vêm do tema / mapa de status — nunca a paleta default do Apex.
- Um gráfico responde uma pergunta ("Quantos caminhões por status hoje?"); título é a pergunta, não "Gráfico 1".
- Números grandes (KPI) primeiro, gráfico depois. Donut só até ~5 fatias; mais que isso, barras horizontais ordenadas.
- Desligue animações em gráficos que atualizam via polling/SSE (`chart.animations.enabled: false`) — movimento constante cansa quem fica o turno olhando.

## Movimento

Mínimo e funcional: transições padrão do Vuetify em dialogs/menus, `v-expand-transition` em painéis. Nada de animação de entrada escalonada em tela operacional. Respeite `prefers-reduced-motion`.

## O que NÃO fazer

- Gradiente roxo, glassmorphism, sombras dramáticas, ilustração decorativa em tela de trabalho.
- Hex novo em `<style scoped>` ou `style="..."` inline.
- Nova fonte, nova biblioteca de UI (Tailwind, PrimeVue) ou novo pacote de ícones — o sistema é Vuetify + MDI.
- Esconder ação importante em menu "⋮" quando há espaço para o botão.
- Tela sem estados de loading/vazio/erro.

Lembre: a melhor interface aqui é a que a portaria usa às 5h da manhã sem pensar. Execute com precisão, não com efeito.

Fontes: [Vuetify — Theme](https://vuetifyjs.com/en/features/theme/) · [Vuetify — Global defaults](https://vuetifyjs.com/en/features/global-configuration/) · [WCAG 2.2](https://www.w3.org/TR/WCAG22/) · [Material Design 3 — Color roles](https://m3.material.io/styles/color/roles) · [Refactoring UI](https://www.refactoringui.com/) (Adam Wathan & Steve Schoger) · [Nielsen Norman — Empty states](https://www.nngroup.com/articles/empty-state-interface-design/)
