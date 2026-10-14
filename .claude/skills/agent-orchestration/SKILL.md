---
name: agent-orchestration
description: "Como orquestrar os subagentes do TruckFlow no Claude Code (Agent tool, fork vs agente novo, paralelismo, background, worktree) com receitas prontas: nova entidade CRUD ponta a ponta, bug em produção, item do SAD, migration em tabela quente, feature mobile + backend. Use quando a tarefa envolve mais de um especialista ou mais de um repo."
version: 1.0.0
tags: [Agents, Orchestration, Multi-Agent, Claude Code, TruckFlow]
---

# Orquestração de agentes — TruckFlow

Times de agentes especializados colaborando numa tarefa complexa. Cada agente tem papel, objetivo e ferramentas (`.claude/agents/*.md`); quem orquestra é você (ou o `project-supervisor-orchestrator`).

## Quando orquestrar

**Orquestre quando:**
- A tarefa cruza repos (API `C:\ESTUDO\TruckFlow`, admin `C:\ESTUDO\TruckFlowApp`, mobile `C:\tf-mobile`)
- Há trabalho independente que pode rodar em paralelo (admin ∥ mobile, pesquisa ∥ implementação)
- A investigação exigiria ler muitos arquivos — delegue e fique só com a conclusão

**Não orquestre quando:**
- É uma mudança num arquivo que você já conhece → faça direto
- É uma busca pontual de um símbolo → Grep/Read direto
- O design não está fechado → rode `grill-me` primeiro; orquestrar dúvida multiplica dúvida

## Primitivas do Claude Code

| Primitiva | Quando usar | Cuidado |
|---|---|---|
| **Agent (tipo nomeado)** — ex. `dotnet-backend-specialist` | Trabalho especializado com prompt próprio do agente | Começa **sem** seu contexto: o prompt precisa ser autossuficiente (formato de handoff abaixo) |
| **Agent `fork`** | Precisa de todo o contexto já levantado na conversa (relatórios, decisões) | Mesmo modelo que você; não re-delegar de dentro do fork |
| **Agent `Explore`** | Varredura ampla read-only ("onde está X em 3 repos") | Localiza, não audita |
| **Várias chamadas Agent na mesma mensagem** | Trabalho independente em paralelo | Nunca dois agentes editando o mesmo arquivo |
| **Background + notificação** | Agentes rodam em background; você é notificado ao terminar | Não invente o resultado antes da notificação; não faça polling |
| **`isolation: "worktree"`** | Refactor arriscado/amplo, experimento descartável | Worktree limpa sozinha se nada mudou; mudanças precisam ser trazidas de volta |
| **SendMessage** | Continuar um agente com o contexto dele (correção, próxima etapa) | Novo Agent começa do zero |
| **Workflow tool** | Fan-out grande e determinístico | **Só se o usuário pedir explicitamente** ("usa um workflow") |

## Formato de handoff (prompt para agente novo)

```markdown
## Contexto       — objetivo de negócio, cliente Aurora, papel do usuário final
## Decisões       — ADRs, respostas do usuário, contrato já definido
## Referências    — caminhos absolutos: padrão a copiar + arquivos a alterar
## Contrato       — rota, verbo, roles, request/response JSON, erros
## Aceite         — verificável; cross-tenant quando houver dado de tenant; build passando
## Fora de escopo — o que não mexer (edits cirúrgicos)
```

## Receitas ("crews")

### 1. Nova entidade CRUD ponta a ponta
```
truckflow-domain-specialist (regras, se houver)        [seq]
  → dotnet-backend-specialist (entidade, migration, DTO, service, controller)   [seq]
  → postgres-pro (revisa migration/índices)            [seq, rápido]
  → qa-expert (teste integração + cross-tenant)        [seq]
  → vue-specialist (cadeia admin) ∥ mobile-expo-specialist (se o motorista usa)  [paralelo]
```
Aceite: listagem paginada (`XxxListQueryDto` + `PagedResponse`), `IEmpresaScoped`, builds verdes.

### 2. Bug em produção
```
debugger (reproduz, causa raiz, evidência)            [seq]
  → especialista da camada (correção mínima)           [seq]
  → qa-expert (teste de regressão que falha antes / passa depois)  [seq]
  → chaos-engineer (se causa for resiliência: 2 instâncias, timeout, SEFAZ)  [condicional]
```
Lembre: `master` da API faz auto-deploy no Railway.

### 3. Item do SAD Aurora
```
risk-manager (classifica: código vs não-código, nível A-D, evidência)  [seq]
  → truckflow-core (gate pré-piloto/pré-rollout, invariantes)          [seq]
  → task-decomposition-expert (tasks Jira no formato do backlog)        [seq]
  → especialistas para executar                                         [conforme tasks]
```
Saída do risk-manager sempre com pergunta + legenda + texto de evidência pronto para colar.

### 4. Migration em tabela quente (Agendamento, Notificacao, NotificacaoEntrega, OutboxEvent)
```
postgres-pro (plano: lock, CONCURRENTLY, backfill em lotes, rollback)  [seq]
  → dotnet-backend-specialist (migration EF + código)                   [seq]
  → qa-expert (migration roda no Testcontainers + testes de concorrência existentes)  [seq]
  → chaos-engineer (deploy com 2 instâncias / migration no startup)     [condicional]
```
Lembre: `db.Database.Migrate()` ainda roda no startup (INFRA-03 pendente).

### 5. Feature mobile + backend
```
truckflow-core (ADR: push/tracking/LGPD)                               [seq]
  → dotnet-backend-specialist (endpoint motorista; MotoristaId do JWT)  [seq]
  → mobile-expo-specialist ∥ vue-specialist (se admin vê o resultado)   [paralelo]
  → risk-manager (se dado pessoal: localização, telefone)               [condicional]
```
Contrato aditivo — build mobile publicado não atualiza junto com a API.

## Referências

- `references/flows.md` — fluxos sequencial, paralelo e condicional com diagramas
- `references/tools.md` — quem tem quais ferramentas e quando escolher cada agente
- `references/troubleshooting.md` — agente fugiu do padrão, inventou endpoint, edits conflitantes, contexto estourado

## Relacionados

Agente `project-supervisor-orchestrator` (executa essas receitas), skill `using-superpowers` (mapa tarefa → skill/agente), skill `grill-me`.
