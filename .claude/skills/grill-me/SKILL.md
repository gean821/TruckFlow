---
name: grill-me
description: Entrevista o usuário sem dó sobre um plano/design até chegar a um entendimento compartilhado, resolvendo cada ramo da árvore de decisão. Use quando o usuário quiser estressar um plano, "ser grelhado" sobre um design (feature, ADR, migration, contrato de API, fluxo de tela), ou disser "grill me" / "me sabatina".
---

# Grill me — sabatina de design (TruckFlow)

Me entreviste implacavelmente sobre cada aspecto deste plano até termos entendimento compartilhado. Percorra cada ramo da árvore de decisão, resolvendo as dependências entre decisões uma a uma. **Para cada pergunta, dê sua resposta recomendada** (com o porquê em 1-2 linhas).

Regras:

1. **Uma pergunta por vez.** Espere a resposta antes da próxima.
2. **Se a pergunta pode ser respondida explorando o código ou os docs, explore em vez de perguntar.** Fontes, nesta ordem:
   - `Docs/adr/*` (decisões aceitas — não re-litigar sem motivo novo; se for o caso, a saída é um ADR novo que substitui o antigo)
   - `Docs/*.md` (backlogs vivos: SAD, Entra ID, NF-e, rastreamento, SSE)
   - `CLAUDE.md` dos 3 repos (`C:\ESTUDO\TruckFlow`, `C:\ESTUDO\TruckFlowApp`, `C:\tf-mobile`)
   - o código real
3. **Ordene pelo que trava o resto**: contrato/dados primeiro (entidade, DTO, migration, tenant), depois fluxo (status, permissões, erros), depois UX/infra.
4. Explicite quando uma decisão é **reversível** (pode decidir rápido) vs **irreversível** (migration destrutiva, contrato público consumido pelo mobile já publicado na loja, dado fiscal/LGPD) — essas merecem mais perguntas.

## Ramos que SEMPRE devem ser cobertos no TruckFlow

- **Multi-tenant**: a entidade é `IEmpresaScoped`? De onde vem o `EmpresaId` (claim, nunca body)? O que acontece com um usuário da Empresa B?
- **Papéis**: quais de `Admin`, `Gerente`, `Monitor`, `Portaria`, `Motorista` podem fazer isso? Motorista nunca usa Entra ID.
- **Concorrência**: duas pessoas/instâncias fazendo isso ao mesmo tempo — `xmin`, unique constraint, `FOR UPDATE SKIP LOCKED`, Idempotency-Key?
- **Estados**: quais transições de status são válidas? Quem dispara? O que é terminal?
- **Erros**: qual exceção de domínio (`BusinessException`/`NotFoundException`/...) e qual HTTP o front/mobile vai receber? Mensagem em PT-BR para o usuário final.
- **Paginação**: se é listagem de tela, nasce paginada (`QueryDto` + `PagedResponse`).
- **Contrato entre repos**: DTO muda? O mobile já publicado quebra? Precisa de versão/campo opcional?
- **LGPD / fiscal**: tem dado pessoal (telefone, localização, CPF) ou fiscal (NF-e)? Retenção, consentimento, auditoria (`AuditLogService`).
- **Escala Aurora (ADR-0001)**: funciona com 21 fábricas e 2+ instâncias da API (nada de estado em memória)?
- **Teste**: qual teste de integração (Postgres real) prova que funciona e que não vaza entre tenants?

## Saída final

Quando os ramos estiverem resolvidos, feche com um resumo curto:
- Decisões tomadas (1 linha cada)
- Riscos aceitos conscientemente
- O que vira ADR (se for decisão arquitetural) e o que vira task (formato Jira, ver agente `task-decomposition-expert`)
