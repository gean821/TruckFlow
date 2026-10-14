# Troubleshooting de orquestração

## Agente fugiu do padrão da casa

**Sintoma:** service static class no admin, `AutoMapper` no backend, `ProblemDetails` novo, `enabled: !!id` estático, query sem paginação.

**Causa:** prompt sem arquivo de referência; agente usou o "padrão da internet".

**Correção:**
- Inclua sempre no handoff o caminho do arquivo canônico a copiar (`UsuarioService.ts` / `usuario.queries.ts` / `ManageUserView.vue`; `GradeController.cs` / `GradeService.cs` com `MapToResponse`).
- Devolva ao mesmo agente via SendMessage: "Refaça seguindo `<arquivo>`; o padrão do projeto é X".
- No admin, rode `pattern_auditor.py` no diff.

## Agente inventou endpoint/campo

**Sintoma:** front chamando `/v1/transportadoras/ativas` que não existe; DTO com campo que a API não devolve.

**Causa:** web/mobile começaram antes do contrato fechado.

**Correção:** pare o cliente; feche o contrato com `dotnet-backend-specialist` (controller + DTO reais); entregue o JSON do contrato no handoff. Regra: **cliente nunca começa antes do contrato escrito**.

## Edits conflitantes

**Sintoma:** dois agentes alteraram o mesmo arquivo; um sobrescreveu o outro; build quebra.

**Correção:**
- Serialize agentes que tocam os mesmos arquivos (ex.: `typescript-pro` e `vue-specialist` nos `*.types.ts`).
- Para refactor amplo, `isolation: "worktree"`.
- Revise `git diff` antes de seguir.

## Vazamento entre tenants passou despercebido

**Sintoma:** endpoint novo retorna dado de outra empresa; query com `IgnoreQueryFilters()` sem `WithTenant`; entidade nova não implementa `IEmpresaScoped`; `Usuario` (não é `IEmpresaScoped`) buscado com `FindByIdAsync` sem checar `EmpresaId`.

**Correção:** todo handoff com dado de tenant leva critério de aceite cross-tenant; `qa-expert` escreve o teste (Empresa A × Empresa B) antes do merge; `truckflow-core` revisa qualquer `IgnoreQueryFilters`/`...AcrossTenants`.

## Contexto estourou

**Sintoma:** agente perde o fio, repete leitura, resposta genérica.

**Correção:**
- Delegue varreduras a `Explore` e fique com a conclusão.
- Handoff com **resumo** do agente anterior, não o transcript.
- Quebre a tarefa (ver `task-decomposition-expert`).
- Resultados grandes de subagente vão para arquivo no scratchpad; passe o caminho.

## Agente "terminou" mas não validou

**Sintoma:** "implementado" sem `dotnet build`/`pnpm run build`/`tsc --noEmit`.

**Correção:** critério de aceite explícito com o comando de validação; reporte falhas como falhas (com saída), não como sucesso.

## Resultado previsto antes da notificação

**Sintoma:** orquestrador descreve o que o agente "provavelmente" fez.

**Correção:** agentes em background notificam ao terminar — espere. Nunca fabrique resultado; se o usuário perguntar, diga que ainda está rodando.

## Doc divergente do código guiou o agente

**Sintoma:** agente reintroduz token em `localStorage`, procura um caminho de arquivo que mudou, ou cria código em `TruckFlow.Services` (projeto vazio, fora da .sln).

**Correção:** handoff aponta o código real (arquivo canônico); corrija o `CLAUDE.md`/`RULES.md` do repo na mesma entrega (ver skill `project-memory`) em vez de só avisar.

## Mudança quebrou o app publicado

**Sintoma:** motoristas com versão antiga do app recebendo erro após deploy da API.

**Correção:** contrato aditivo (campo novo opcional, endpoint novo); nunca renomear/remover/mudar tipo sem depreciação; enum novo exige fallback no cliente. `fullstack-developer` é dono dessa checagem.

## Commit com atribuição ao Claude

**Sintoma:** `Co-Authored-By: Claude ...` ou `Claude-Session: ...` em commit/PR.

**Correção:** regra do projeto (CLAUDE.md + memória): nunca incluir. Corrija a mensagem antes do push.
