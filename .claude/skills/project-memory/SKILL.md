---
name: project-memory
description: "Memória do projeto TruckFlow: onde cada tipo de conhecimento mora (ADR, backlog vivo em Docs/, CLAUDE.md por repo, RULES.md, auto-memory do Claude), em que ordem consultar, quando registrar e como corrigir doc que diverge do código. Use ao tomar/registrar decisão arquitetural, ao encontrar doc que contradiz o código, ao final de uma investigação que gerou conhecimento não-óbvio, ou quando o usuário pedir 'lembra disso'."
---

# Memória do projeto (TruckFlow)

Memória não é armazenar — é **recuperar a coisa certa na hora certa**. Quando um agente "esquece" uma decisão ou dá resposta inconsistente com o que já foi decidido, quase sempre é falha de recuperação (leu a fonte errada, ou a fonte estava defasada), não falta de informação.

## Onde cada coisa mora

| Tipo de conhecimento | Lugar | Muda como | Exemplo |
|---|---|---|---|
| **Decisão arquitetural** (com contexto e alternativas) | `Docs/adr/NNNN-*.md` — template Michael Nygard (Status, Data, Contexto, Decisão, Consequências, Alternativas, Referências) | **Nunca edita histórico.** Mudou de ideia → ADR novo com "Substitui ADR-XXXX" e o antigo vira "Substituído por" | ADR-0002 notificações; ADR-0004 gates pré-piloto |
| **Backlog / plano vivo** | `Docs/*.md` (`sad-aurora-backlog.md`, `entra-id-integracao-backlog.md`, `nfe-distribuicao-dfe-backlog.md`, `rastreamento-motorista-backlog.md`, `notificacoes/sse-implementacao.md`, `divisao-tasks-sprint13.md`) | Atualiza à medida que executa (✅, datas, status final) | Revisão do ADR-0003 sem TimescaleDB vive em `rastreamento-motorista-backlog.md` |
| **Convenção estável de um repo** | `CLAUDE.md` na raiz de cada repo | Edição direta, curta | Stack, comandos, estrutura |
| **Regras de código detalhadas (backend)** | `.claude/RULES.md` | Edição direta | Padrão controller → service → MapToResponse |
| **Conhecimento de especialista** | `.claude/agents/*.md`, `.claude/skills/*` | Edição direta | Cadeia types → service → queries → hooks |
| **Preferência do usuário / feedback / fato de projeto não derivável do código** | auto-memory: `C:\Users\User\.claude\projects\C--ESTUDO-TruckFlow\memory\` (um fato por arquivo, frontmatter `name/description/metadata.type`, índice em `MEMORY.md`) | Atualiza o arquivo existente; apaga se ficou errado | "edits cirúrgicos", "sem atribuição ao Claude", "Railway auto-deploy na master" |
| **Decisão pequena de implementação** | Mensagem de commit / descrição do PR | Imutável | "Trocado fallback do filtro de tenant para zero linhas" |
| **Fato derivável do código** | **Lugar nenhum** — leia o código | — | Nome de classe, assinatura, lista de DbSets |

ADRs de cada cliente: o admin tem `C:\ESTUDO\TruckFlowApp\docs\adr\` (notificações in-app, mapa tracking, WhatsApp) e o mobile `C:\tf-mobile\docs\adr\` (0001-0004, incl. `0002-captura-localizacao.md`). Os do backend (`Docs/adr/`) são a referência macro.

## Ordem de recuperação

Ao começar uma tarefa que toca design (auth, notificações, tracking, multi-tenant, NF-e):

1. `Docs/adr/*` do backend — decisão aceita vence opinião.
2. Doc de backlog vivo do tema — pode ter **revisado** o ADR (ex.: tracking sem TimescaleDB).
3. **Código real** — é a verdade do *estado atual*.
4. `CLAUDE.md` do repo + `.claude/RULES.md` — convenções.
5. Auto-memory (`MEMORY.md`) — contexto do usuário e decisões fora do código.

Diferencie sempre **estado atual** (código) de **estado alvo** (ADR/backlog). Divergência entre os dois é informação, não erro de leitura.

## Doc divergindo do código: corrija a fonte

Preferência do usuário: **corrigir a fonte, não espalhar avisos de "documentação defasada"** em agentes, skills ou respostas.

Quando um doc contradiz o código:
1. **Não "corrija" o código para bater com o doc** sem confirmar qual está certo (código = estado atual; ADR/backlog = estado alvo).
2. Se é `CLAUDE.md` ou `RULES.md` descrevendo o estado atual errado → **edite o arquivo na mesma entrega** (edit cirúrgico: só o trecho que mudou).
3. Se é doc de backlog vivo → atualize o status/caminho.
4. Se é ADR → não edite o histórico; proponha ADR novo que substitui (ou registre a revisão no backlog do tema, como foi feito com tracking).
5. Mencione ao usuário em uma linha o que foi ajustado.

Regra de fechamento de tarefa: ao terminar algo que um `CLAUDE.md` descreve ("em implementação", caminho de arquivo, decisão de auth), atualize o `CLAUDE.md` daquele repo junto.

## Quando registrar o quê

- Decidiu algo com alternativas descartadas e consequência de longo prazo → **ADR** (ex.: pendente `ENTRA-10` = ADR-0005 do SSO federado por tenant).
- Executou parte de um backlog → atualize o **doc vivo** (✅ + data + achados, como o NF-e fez com os 6 bugs do Zeus).
- Usuário corrigiu seu jeito de trabalhar ou confirmou uma abordagem → **auto-memory feedback** (com **Why** e **How to apply**).
- Fato de projeto que não está no código nem no git (prazo, cliente, restrição contratual) → **auto-memory project**, datas absolutas.
- Convenção nova que todo agente precisa seguir → `CLAUDE.md` ou `RULES.md`.

## Anti-padrões

### ❌ Guardar tudo para sempre
Memória cheia de fato velho é pior que memória vazia: o agente recupera a versão errada com confiança. Apague/atualize memória que deixou de ser verdade.

### ❌ Duplicar fato do código na memória
"O `GradeService` tem o método X" apodrece no próximo refactor. Memória guarda o **não-óbvio** (por que, restrição, preferência), o código guarda o óbvio.

### ❌ Editar ADR aceito
Apaga o aprendizado. ADR novo substitui.

### ❌ Doc divergente deixado como está
Se você percebeu e não corrigiu a fonte (CLAUDE.md/RULES.md/backlog), o próximo agente vai cair no mesmo erro. E nota de "defasado" espalhada em agente/skill não resolve — corrija o doc.

### ❌ Memória de conversa
Estado de uma tarefa em andamento não é memória — é contexto da conversa.

## ⚠️ Arestas afiadas

| Problema | Severidade | Solução |
|---|---|---|
| ADR diz uma coisa, backlog revisou | crítica | Leia sempre o backlog do tema depois do ADR; backlog datado mais recente vence para *alvo* |
| CLAUDE.md desatualizado induz código errado (ex.: reintroduzir localStorage) | alta | Código vence para *estado atual*; atualize o CLAUDE.md na mesma entrega |
| Memória cita arquivo/flag que não existe mais | alta | Verifique no código antes de recomendar |
| Mesmo fato em 3 lugares divergindo | média | Um dono por fato (tabela acima); os outros linkam |
| Datas relativas ("semana que vem") | média | Sempre data absoluta (AAAA-MM-DD) |
| Nomes de pessoas em docs/memória | média | Prefira papel (dev sênior, dev júnior, TI Aurora) |

## Skills relacionadas

`grill-me` (fechar decisões antes de registrar), agente `truckflow-core` (guardião dos ADRs), agente `task-decomposition-expert` (backlogs).
