# Análise de robustez — Jira, Grade/Agendamento, Mobile e features (2026-10-17)

Consolidação de 4 investigações pedidas em conjunto: estado real do backlog Jira (`TRUCK`),
auditoria de bugs/cobertura de teste em Grade e Agendamento, auditoria de design/responsividade
do mobile, e cruzamento com o backlog de melhorias já desenhado em
`Docs/implantacao-melhorias-backlog.md`. Nada aqui foi implementado ainda — é diagnóstico +
proposta de priorização.

## 0. Link do Instagram

Não consegui extrair conteúdo do reel (`instagram.com/reel/DehcVvJRpFV`) — é da conta
`castrologic.tech`, sem legenda com lista de features, Instagram bloqueia esse tipo de leitura
automática. Se tiver algo específico visto ali (print, lista, transcrição), manda que eu incorporo.

## 1. Jira (`TRUCK`) — o que já existe vs. o que está divergente do código real

O backlog já é grande: 257 issues. A maior parte (TRUCK-1 a TRUCK-196) é histórico de
implementação já feito. A partir de TRUCK-197 existe um backlog SAD/segurança bem estruturado
(`Docs/sad-aurora-backlog.md`), organizado em épicos: `SEC`, `IAM`, `CI`, `TEST`, `INFRA`, `OBS`,
`CRYPTO`, `MOB`, `WEB`, `DATA`, `NET`, `VULN`. A maioria ainda está "To Do".

**3 divergências reais encontradas ao comparar Jira com o código (não suposição — confirmado lendo
o código):**

| Issue | Status Jira | Realidade no código | O que fazer |
|---|---|---|---|
| **TRUCK-235** (WEB-03 — relatórios reais) | **Done** | `src/views/Relatorio.vue:265-278` continua exatamente o mock descrito no ticket: `setTimeout` + `console.log`, zero chamada de API, zero arquivo gerado. | Reabrir. Esse item se funde com o épico de exportação (`IMPL-02..06` do backlog já desenhado) — ver seção 4. |
| **TRUCK-222** (INFRA-03 — migrations fora do `Program.cs`) | **Done** | `Program.cs:169` chama `MigrationLockRunner.MigrateComLock(db)` — ainda dentro do processo da API no boot. O que foi feito foi o **advisory lock** (evita 2 instâncias competirem), não a separação em job de deploy que o `ADR-0004` item 2 pede literalmente ("Mover migrations para job separado no pipeline de deploy"). | Reabrir ou criar um follow-up específico — isso é pré-requisito de rollout multi-fábrica (ADR-0004), ou seja, bloqueia ir de 1 → 21 fábricas. |
| **TRUCK-211** (IAM-01 — rate limit login + posição) | To Do | A metade de **login** já foi implementada nesta sessão (`SECURITY.md`, `AddRateLimitingDependencyInjection.cs`) — só não está refletido no Jira. A metade de **posição do motorista** é N/A hoje: não existe nenhum endpoint de posição/tracking no código (feature do ADR-0003 ainda não construída). | Comentar no ticket que a parte de login está feita; manter aberto só pela parte de posição (que entra junto quando o tracking for implementado). |

**2 confirmações importantes — não recriar:**
- **TRUCK-194** já é o Epic/Task de push notifications ("In Progress"). Quando formos atacar o
  backlog `PUSH-01..08`, as tasks devem ser subtasks dele, não itens novos soltos.
- **TRUCK-233** (MOB-04 — desregistrar push token no logout) já existe e é **exatamente** o que eu
  tinha desenhado como `PUSH-02`/`PUSH-03`. Confirmei no código: `DispositivosController.cs` só
  tem `POST /v1/dispositivos/registrar`, não existe endpoint de desregistro — o gap é real.

**1 bug real, pequeno e barato, parado em "To Do":**
- **TRUCK-256**: `react-native-gesture-handler` (peer dependency nativa do
  `@react-navigation/drawer`) está ausente no mobile. Não quebra no Expo Go (já vem embutido), mas
  **pode crashar em build standalone EAS** — ou seja, o motorista pode abrir o app instalado de
  verdade e crashar no menu drawer. Fix de 1 linha (`npx expo install react-native-gesture-handler`)
  + validação em device real. Dado que o foco agora é robustez do mobile, isso deveria subir de
  prioridade — é o tipo de bug que só aparece depois de publicado.

## 2. Grade e Agendamento — resposta direta à sua pergunta

**Não, não está "tudo certinho".** A parte matemática (`Grade.GerarSlots`) é sólida e testada. A
parte de negócio (`GradeService` — criar/editar/excluir grade) está **quase sem teste nenhum** e
tem bugs reais de integridade de dados, alguns sérios.

### Cobertura de testes hoje
- `GradeTests.cs`: 11 testes, só de `Grade.GerarSlots` (entidade pura). Todos passam.
- `GradeService.CreateGrade` / `UpdateGrade` / `DeleteGrade`: **0% de cobertura** — nenhum teste
  unitário nem de integração.
- `GradeCreateValidator` / `GradeUpdateValidator`: nenhum teste.
- **Nenhum teste de integração prova a exclusion constraint (`agendamento_no_overlap_per_doca`)
  no fluxo de Grade** — só no fluxo de reserva direta de agendamento (`AgendamentoReservaConcorrenciaTests`).

### Bugs reais encontrados (arquivo:linha no relatório completo do agente, resumo aqui)

**Críticos (risco de dado/operação):**
1. **`DeleteGrade` apaga entregas finalizadas sem auditoria.** O bloqueio de exclusão só olha
   agendamentos `Pendente`/`Agendado`/`EmAndamento` — `Finalizado` (entrega real já aconteceu) não
   bloqueia nada. A exclusão é um `ExecuteDeleteAsync` em massa que **não passa pelo
   `AuditSaveChangesInterceptor`**, então não gera log de auditoria. Cenário: admin exclui uma
   grade antiga pensando estar limpando algo obsoleto e apaga, sem rastro, centenas de entregas
   reais já confirmadas.
2. **`UpdateGrade` não é transacional** entre apagar slots disponíveis antigos e gerar os novos —
   se o `SaveChanges` final falhar (ex: conflito de concorrência), a grade fica **com zero vagas**
   até alguém editar de novo manualmente.
3. **Qualquer PATCH regenera o período inteiro da grade**, não só o futuro — duplica histórico
   (slots `Expirado` antigos convivem com novos `Disponivel` recriados para as mesmas datas já
   passadas).

**Moderados (inconsistência/UX):**
4. Conflito de slot: `CreateGrade` lança erro explicando a colisão; `UpdateGrade` descarta o slot
   em silêncio, sem avisar o admin.
5. Grade pode nascer com **zero vagas** (intervalo maior que a janela de horário) e ainda assim
   retorna `201 Created` como se tivesse funcionado.
6. Virada de meia-noite permitida no create, bloqueada no update (mesma regra, validators
   divergentes).
7. PATCH parcial (só `HoraFinal`, por exemplo) não valida contra o `HoraInicial` já salvo.
8. Nenhuma validação contra grade com `DataInicio` no passado.
9. `LocalDescargaId` inválido vira erro 404 em vez de 400 de validação.

**Ponto positivo, pra não inflar o quadro:** a proteção de concorrência na doca (constraint de
exclusão no banco + pré-check em app + catch do erro do Postgres) é bem desenhada e coerente — só
falta prova automatizada específica para o fluxo de Grade.

### Proposta
Novo épico **GRADE** no Jira, antes de qualquer feature nova nessa área:
- `GRADE-01` (P0): corrigir `DeleteGrade` para bloquear exclusão com entregas finalizadas + testes.
- `GRADE-02` (P0): tornar `UpdateGrade` transacional (`BeginTransactionAsync`) + teste de falha simulada.
- `GRADE-03` (P0): regenerar só slots futuros no PATCH, não o período inteiro + teste.
- `GRADE-04` (P1): alinhar validators create/update (virada de meia-noite, PATCH parcial, zero vagas, data retroativa, `LocalDescargaId`).
- `GRADE-05` (P1): suíte de testes de integração para `GradeService` (Postgres real) cobrindo os 3 bugs críticos + concorrência na geração de slots.

## 3. Mobile — design e responsividade (achados do agente, ranqueados)

1. **Header duplicado** — `Navbar` global + header próprio em quase toda tela; rouba ~15-20% da altura em celular pequeno, inclusive sobre o scanner de câmera.
2. **Responsividade zero** — nenhuma adaptação de layout a tamanho de tela (`Dimensions`/breakpoints não usados em lugar nenhum); `app.json` diz suportar tablet mas não adapta nada.
3. **Cor de marca (`#195FA0`) hardcoded 99x em 23 arquivos** — existe um design system (gluestack/Tailwind) só que configurado com paleta cinza, não a cor real da marca. Trocar a cor hoje = editar 23 arquivos na mão.
4. **"QR Code" do ticket não é QR Code** — é um ícone decorativo esticado; a tela diz "apresente este código na portaria" mas não há nada escaneável de fato. É a peça mais visível do fluxo, hoje só simulada.
5. **Telas mortas** (`confirmacao.tsx`, `formulario.tsx`) — componentes vazios; se alguma rota navegar pra lá, usuário vê tela branca.
6. Lista de agendamentos usa `ScrollView.map()` em vez de `FlatList`, ao contrário da própria convenção do projeto.
7. **4 implementações de modal diferentes** convivendo (gluestack, RN nativo x2 duplicado, `TouchableWithoutFeedback`).
8. Botões reinventados com `Pressable` + classe hardcoded em vez do componente `Button` do design system (2 telas).
9. Scanner de câmera: tela de permissão negada usa botão nativo não estilizado, sem fallback para abrir configurações do app.
10. **Zero acessibilidade** — nenhum `accessibilityLabel`/`accessibilityRole` no repo inteiro.
11. Zustand sem seletor em 4 telas-chave — qualquer mudança no store de auth re-renderiza a tela inteira.
12. Contraste baixo em placeholder de dado vazio (perfil).
13. Emoji como ilustração de estado vazio, inconsistente com o resto do app (que usa ícones lucide).
14. Logout usa `router.push` em vez de `router.replace` na Home — tela autenticada fica acessível ao voltar.
15. Logo da navbar com dimensão fixa "no olho" (`width:180, marginVertical:-40`) — aperta ações à direita em tela estreita.
16. `ActionCard` com altura fixa e título sem truncamento — pode quebrar/cortar em tela estreita.

**Recomendação de abordagem**: qualquer "redesign" só se sustenta se o primeiro passo for migrar a
cor de marca e os hex espalhados para os tokens do tema já existente (gluestack/Tailwind) — hoje
não existe um lugar único pra mudar a aparência do app. Depois disso, atacar #1 (header duplicado),
#4 (QR Code real) e #5 (telas mortas) como os 3 itens de maior impacto visível.

## 4. Robustez — relatórios, importação, exportação

Confirmei no código: **relatórios são 100% mock** (seção 1), e **não existe nenhuma funcionalidade
de importação** em lugar nenhum do admin hoje (procurei por "importar"/"xlsx"/"planilha" — zero
resultado fora da própria tela de relatório e da landing). Ou seja, os dois pontos que você
apontou como "coisas que um sistema robusto tem" são gaps reais, não achismo.

Isso já está desenhado em detalhe em `Docs/implantacao-melhorias-backlog.md` (`IMPL-02..06` para
exportação streaming CSV/XLSX de Auditoria/Agendamentos/Recebimentos, `IMPL-07..13` para
importação assíncrona robusta com job + worker + polling/SSE + template `.xlsx`). A única correção
agora: **`IMPL-02..06` deve absorver/substituir o TRUCK-235 (WEB-03)** em vez de conviver como
coisas separadas — é o mesmo problema (relatório mock), só que o backlog novo trata o escopo certo
(todas as páginas, não só Agendamento).

## 5. Ordem de prioridade geral (cruzando tudo)

**P0 — corrigir antes de empilhar feature nova (risco de dado/operação real, barato de corrigir):**
1. `GRADE-01/02/03` — os 3 bugs críticos de Grade (perda de histórico, grade zerada, duplicação).
2. `TRUCK-256` — peer dependency do mobile (crash em build real).
3. Reabrir/clarificar `TRUCK-222` — migrations fora do `Program.cs` é pré-requisito literal do ADR-0004 para sair de 1 fábrica (hoje) para as 21 da Aurora.

**P1 — já estava priorizado, confirmado que vale manter no topo:**
4. `PUSH-01..08` (como subtasks de `TRUCK-194`/`TRUCK-233`) — push notifications.
5. `GRADE-04/05` — testes e consistência dos validators de Grade.
6. Reabrir `TRUCK-235` → absorver em `IMPL-02..06` — relatórios reais.

**P2 — mobile (seu pedido explícito de foco):**
7. Migrar cor de marca pro tema (pré-requisito de qualquer redesign).
8. Header duplicado, QR Code real, telas mortas.
9. Resto dos achados de mobile (acessibilidade, FlatList, modais, etc).

**P3 — expansão de feature (médio prazo, maior esforço):**
10. `IMPL-07..13` — importação assíncrona (planilha → fila → worker).
11. `IMPL-15` — cargos/perfis dinâmicos por fábrica (ADR primeiro).
12. `IMPL-01` — landing page.
13. `IMPL-14` — storage de fotos (bloqueado no contrato Azure).

## 6. O que falta pra eu criar isso no Jira de fato

Preciso de um "ok" seu porque isso envolve **corrigir status de issues já marcadas Done** (TRUCK-235,
possivelmente TRUCM-222), o que é visível pra qualquer outra pessoa olhando o board, e **criar um
épico novo** (GRADE) com 5 tasks. Se topar, eu crio na ordem de prioridade da seção 5, usando os
épicos certos como parent (nada de duplicar TRUCK-194/233).
