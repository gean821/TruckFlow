# Fluxos de orquestração

Três formas básicas — e a regra que governa todas: **contrato e dados primeiro, clientes depois**.

## 1. Sequencial

Cada etapa depende da saída da anterior. Use quando há dependência de contrato.

```mermaid
sequenceDiagram
  participant O as Orquestrador
  participant D as truckflow-domain-specialist
  participant B as dotnet-backend-specialist
  participant Q as qa-expert
  O->>D: regra de negócio (status, janela, quem pode)
  D-->>O: invariantes + transições
  O->>B: implementar com invariantes (handoff)
  B-->>O: contrato final + arquivos
  O->>Q: testes de integração (Postgres real, cross-tenant)
  Q-->>O: testes verdes / falhas
```

Repasse **resumos** (contrato, arquivos, pendências), não transcripts.

## 2. Paralelo (fan-out / fan-in)

Etapas independentes na **mesma mensagem** (várias chamadas Agent num bloco). Use para repos diferentes depois do contrato fechado, ou para pesquisas independentes.

```mermaid
flowchart LR
  C[Contrato da API fechado] --> W[vue-specialist<br/>admin]
  C --> M[mobile-expo-specialist<br/>motorista]
  C --> T[typescript-pro<br/>tipos compartilhados]
  W --> J[Consolidação:<br/>campos batem com a API?<br/>builds verdes?]
  M --> J
  T --> J
```

Regras:
- Nenhum arquivo editado por dois agentes simultâneos.
- Se `typescript-pro` e `vue-specialist` mexem nos mesmos `*.types.ts`, serialize-os.
- Exploração read-only pode ser paralela sem restrição.

## 3. Condicional

Ramos dependem do achado de uma etapa.

```mermaid
flowchart TD
  A[debugger: causa raiz] --> B{Onde está?}
  B -->|query/lock/índice| P[postgres-pro]
  B -->|service/controller/DI| N[dotnet-backend-specialist]
  B -->|cache/reatividade web| V[vue-specialist]
  B -->|app motorista| M[mobile-expo-specialist]
  B -->|SEFAZ/certificado| S[sefaz-nfe-specialist]
  B -->|2 instâncias / timeout / worker| C[chaos-engineer]
  P & N & V & M & S & C --> R[qa-expert: teste de regressão]
```

## 4. Hierárquico (supervisor)

Para épicos, o `project-supervisor-orchestrator` decide a sequência e delega; especialistas não chamam outros especialistas (exceto `fullstack-developer`, que pode fazer fan-out por repo).

```mermaid
flowchart TD
  U[Pedido do usuário] --> S[project-supervisor-orchestrator]
  S -->|falta info crítica| Q[1 pergunta ao usuário]
  S --> K[truckflow-core: gates/ADR]
  K --> T[task-decomposition-expert: tasks]
  T --> E1[dotnet-backend-specialist]
  E1 --> E2[vue-specialist]
  E1 --> E3[mobile-expo-specialist]
  E2 & E3 --> S
  S --> F[Envelope de status + resumo ao usuário]
```

## 5. Isolado (worktree)

Para refactor amplo/arriscado (ex.: unificar `*Response` vs `*ResponseDto`, mover interfaces de repositório para Domain, trocar envelope de erro por ProblemDetails):

```mermaid
flowchart LR
  A[Agent com isolation: worktree] --> B[Branch própria em worktree temporária]
  B --> C{Build + testes passam?}
  C -->|sim| D[Revisão do diff pelo orquestrador]
  C -->|não| E[Descarta — repo principal intacto]
```

## Escolha rápida

| Situação | Fluxo |
|---|---|
| Front depende de endpoint novo | Sequencial (API → front) |
| Admin e mobile consomem o mesmo endpoint pronto | Paralelo |
| Não sei onde está o bug | Condicional a partir do `debugger` |
| Épico com 10+ tasks | Hierárquico |
| Refactor transversal | Isolado em worktree |
