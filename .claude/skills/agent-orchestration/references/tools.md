# Agentes, ferramentas e quando escolher cada um

## Inventário

| Agente | Ferramentas | Modelo | Escolha quando | Não escolha quando |
|---|---|---|---|---|
| `truckflow-core` | Read, Write, Edit, Glob, Grep, Bash, WebFetch, WebSearch | opus | "Pode fazer X?", invariantes, ADR, multi-tenant, revisão arquitetural | Implementação rotineira |
| `truckflow-domain-specialist` | Read, Write, Edit, Glob, Grep, Bash | sonnet | Regra de agendamento/grade/recebimento/conferência/planejamento, transições de status | Detalhe técnico de framework |
| `dotnet-backend-specialist` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | sonnet | Controller, service, DTO, validator, exceção, DI, worker | Tuning de query/índice puro (→ `postgres-pro`) |
| `postgres-pro` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | sonnet | Migration, índice, lock, `EXPLAIN ANALYZE`, constraint, particionamento | Lógica de negócio |
| `sefaz-nfe-specialist` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | sonnet | NF-e, SEFAZ, Zeus, certificado A1, chave de acesso, XML | Fluxo de agendamento sem NF |
| `vue-specialist` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | sonnet | Cadeia types→service→queries→hooks, Vue Query, reatividade, performance | Decisão visual/UX (→ `frontend-developer`) |
| `frontend-developer` | Read, Write, Edit, Bash, Glob, Grep, WebFetch | sonnet | Tela/feature completa no admin, UX, a11y | Bug de cache sutil |
| `mobile-expo-specialist` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | sonnet | Qualquer coisa em `C:\tf-mobile` | Admin web |
| `typescript-pro` | Read, Write, Edit, Bash, Glob, Grep, WebFetch | sonnet | Espelhar DTO, tipos genéricos, eliminar `any`, env tipado | C# |
| `fullstack-developer` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, Agent | sonnet | Feature nos 3 repos com contrato coerente | Mudança de uma camada só |
| `debugger` | Read, Write, Edit, Bash, Glob, Grep | sonnet | Stack trace, comportamento estranho, causa raiz | Feature nova |
| `qa-expert` | Read, Grep, Glob, Bash, Write, Edit | sonnet | Estratégia de teste, gate cross-tenant, concorrência, cobertura | Escrever produção |
| `chaos-engineer` | Read, Write, Edit, Bash, Glob, Grep | sonnet | Multi-instância, workers, SKIP LOCKED, SEFAZ fora, PG failover, deploy | Bug funcional simples |
| `risk-manager` | Read, Write, Edit, Bash, Glob, Grep, WebFetch, WebSearch | opus | LGPD, SAD Aurora, pen-test, compliance fiscal | Código |
| `project-supervisor-orchestrator` | Read, Write, Agent | sonnet | Pedido multi-agente que precisa de roteamento | Tarefa de um especialista só |
| `task-decomposition-expert` | Read, Write, Glob, Grep | sonnet | Épico → tasks Jira | Executar as tasks |

(Ferramentas conforme o frontmatter de cada `.claude/agents/*.md` — o arquivo é a fonte de verdade.)

## Agentes embutidos do Claude Code

| Agente | Uso no TruckFlow |
|---|---|
| `Explore` | Varrer os 3 repos atrás de um padrão ("todas as views que importam `XService` direto") — devolve conclusão, não dumps |
| `Plan` | Plano de implementação de uma mudança grande antes de delegar |
| `general-purpose` | Pesquisa aberta multi-etapa sem especialista óbvio |
| `fork` | Continuar trabalho que depende de tudo que já está na conversa |

## Ferramentas por tipo de trabalho

| Trabalho | Ferramenta preferida |
|---|---|
| Ler/editar arquivo conhecido | Read / Edit |
| Achar símbolo | Grep |
| Achar arquivos por padrão | Glob |
| Build/test | Bash: `dotnet build`, `dotnet test`, `pnpm run build`, `npx tsc --noEmit` |
| Doc oficial (EF Core, Npgsql, Vue Query, Expo, SEFAZ) | WebFetch / WebSearch |
| Testar admin rodando | skill `webapp-testing` (Playwright) |
| Scaffold de entidade no admin | `python .claude/skills/senior-frontend/scripts/feature_scaffolder.py` |
| Auditoria de padrão no admin | `python .claude/skills/senior-frontend/scripts/pattern_auditor.py` |

## Escolha de modelo

- Decisões com consequência contratual/jurídica (risco, invariantes) → modelo mais forte (`opus`).
- Implementação seguindo padrão claro → `sonnet`.
- Não sobrescreva o modelo do agente sem motivo; o frontmatter já define.
