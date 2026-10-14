---
title: v-if para o raro e caro, v-show para o que alterna muito
impact: MEDIUM
impactDescription: dialogs pesados não montam até abrir; tabs não remontam
tags: rendering, v-if, v-show, dialogs
---

## v-if para o raro e caro, v-show para o que alterna muito

**Impacto: MEDIUM (dialogs pesados não montam até abrir; tabs não remontam)**

- `v-if`: não monta nada enquanto falso. Use em **dialogs/drawers** pesados com queries dentro (`AuditDrawer`, `RelatorioDialog`) — senão a query do drawer roda já no mount da página.
- `v-show`: monta uma vez e só alterna `display`. Use em painéis/abas que o usuário troca toda hora.

**Incorreto (drawer de auditoria dispara query de auditoria assim que a tela abre):**

```vue
<AuditDrawer v-model="auditOpen" entity-name="Administrador" :entity-id="auditId" />
```

**Correto:**

```vue
<AuditDrawer v-if="auditId" v-model="auditOpen" entity-name="Administrador" :entity-id="auditId" />
```

Alternativa: deixar montado e garantir `enabled: () => open.value && !!unref(id)` na query do drawer.

Referência: [Vue — v-if vs. v-show](https://vuejs.org/guide/essentials/conditional.html#v-if-vs-v-show)
