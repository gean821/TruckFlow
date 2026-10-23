# Backlog técnico — SAD Aurora (formulário `formularios_sad_181_00`)

> Rascunho de trabalho, não é um ADR. Objetivo: virar tasks no Jira assim que o MCP for conectado — cada bloco de tarefa abaixo já está no formato "pronto para virar ticket" (título, contexto, onde mexer, como implementar, critério de aceite, esforço).
>
> Repos: **backend** = este repo (`TruckFlow/`) · **mobile** = `C:\tf-mobile\truckflow-driver-app` · **admin** = `C:\ESTUDO\TruckFlowApp\truckflow.app`

---

## 0. Calibração — o que o exemplo da concorrente me ensina

Abri o `formularios_sad_181_00 (2).xlsx` (outra empresa avaliada pela Aurora) e fui direto na aba `Resultado`:

| Categoria | Atendimento do pilar | Faixa de risco |
|---|---:|---|
| GRC | 46,4% | — |
| IAM | 24,6% | — |
| AppSec & DataSec | 23,0% | — |
| SecOps | 17,9% | — |
| Continuidade & Infra | 56,7% | — |
| RH | 49,1% | — |
| Arquitetura | 45,6% | — |
| Mudanças & Roadmap | 34,4% | — |
| Usabilidade | 16,4% | — |
| Relatórios & Saída | 17,2% | — |
| Suporte (SLA) | 0% | — |
| Tecnologias de Proteção | 51,8% | — |
| **Geral** | **34,29 / 100** | **Crítica (faixa 0–40)** |

Ponto-chave: essa empresa **preencheu quase todos os 113 itens com A/B/C/D** (muitos sem evidência anexada na coluna E — só a letra) e ainda assim caiu na faixa **"Crítica"**, a pior do ranking da Aurora. Ou seja:

1. O rigor da planilha não é sobre "preencher tudo com letra" — é sobre **peso**: cada pergunta vale um score fixo dentro do pilar, e blank conta como zero independente de quão bem você justificaria por escrito.
2. Preencher com uma letra sem evidência **não é incomum nesse processo** — a concorrente fez isso em ~40 dos 113 itens. Isso não é o mesmo que inventar uma certificação que não existe (isso é risco jurídico real, ver seção 2). É diferente de forçar "D" em algo que não existe.
3. Nosso objetivo realista **não é bater 80+/100** — é sair de "Crítica" para "Alta" ou "Média" nos pilares que controlamos por código (IAM, AppSec, Arquitetura), e ser transparente nos que não controlamos (GRC institucional, RH, Suporte, Jurídico).

A boa notícia: no pilar onde a concorrente é mais fraca (**AppSec 23%, SecOps 18%, Usabilidade 16%**), a maior parte dos gaps do TruckFlow é **código, não política** — dá pra fechar com trabalho de engenharia em semanas, não em auditoria externa.

---

## 1. Itens jurídicos / comerciais / RH — NÃO viram task de código

Estes 24 itens do questionário (numeração da aba `Requisitos`) não são resolvidos escrevendo código. Separei por quem precisa decidir, com um rascunho de resposta curta (no estilo direto que a própria concorrente usou) para você validar ou ajustar. **Nada aqui foi marcado como task no Jira.**

### 1.1 Jurídico / Compliance — bloqueantes prováveis

| # | Pergunta | Rascunho de resposta | Por que é bloqueante |
|---|---|---|---|
| 10 | DPO (Data Protection Officer) nomeado? | Se ainda não existe: nomear alguém (pode ser o próprio responsável técnico/fundador, formalizado por e-mail interno) **antes de enviar**. Nível `A` ("nomeado, sem atuação ampla ainda") é honesto e rápido de conseguir. | Devidamente devidamente avaliado em toda due diligence LGPD — item recorrente em contratos de dado pessoal. |
| 17 | Apólice de seguro de responsabilidade cibernética? | Cotar antes de enviar. Se não houver orçamento agora, responder em branco é mais seguro que inventar valor de cobertura. | Item que pesa em contratos de porte Aurora; mas **não é code — é decisão financeira/jurídica**. |
| 101 | Retenção de dados pós-contrato — por quanto tempo? | Precisa virar cláusula contratual. Sugestão técnica de base: 30 dias após rescisão, depois purge automático (dá pra implementar — ver `DATA-03` na seção 3). | Decisão jurídica que **define** o comportamento técnico, não o contrário. |
| 74 | Background check para posições sensíveis (DBA, dev com acesso a produção)? | Decisão de RH/contratação. Se a equipe é pequena e não há processo formal, responder em branco é mais seguro do que declarar algo que não existe. | Risco reputacional se auditado e não encontrado. |

### 1.2 Comercial / Contratual — Aurora vai negociar isso com vocês, não é seu código que resolve

| # | Pergunta | Rascunho / orientação |
|---|---|---|
| 75 | SLA de disponibilidade da solução | Não prometa um número sem ter monitoramento pra sustentar (ver `OBS-01`). Sugestão: **99% ("melhor esforço")** até ter observabilidade real; revisar depois. |
| 85 | Comunicação de janelas de manutenção/downtime | Defina um canal simples (e-mail para o contato Aurora + aviso no app/admin) — é decisão de processo, não precisa ser sofisticado ainda. |
| 87 / 88 | Roadmap comunicado / clientes votam features | Responder com honestidade: hoje é reativo (conversas diretas), sem portal público. Nível `A`. |
| 95 | Custos de extração de dados ao fim do contrato | Decisão comercial — sugestão: sem custo adicional (constrói confiança, e teoricamente é barato de entregar tecnicamente — ver `DATA-01`). |
| 97 | Formatos e prazo de disponibilização de dados | Depois que `DATA-01` (seção 3) existir, essa resposta fica fácil: "CSV/JSON via exportação sob demanda, em até X dias úteis". |
| 102/103/104 | Canais de suporte, SLA de tickets, CSM dedicado | Formalizar o que já existe hoje na prática (provavelmente WhatsApp/e-mail direto com a equipe). Não precisa ser 24x7 — só precisa ser **verdadeiro**. |

### 1.3 RH / Processo interno — sem código, mas sem burocracia também

| # | Pergunta | Rascunho / orientação |
|---|---|---|
| 3 | C-Level aprova políticas, com que frequência? | Numa equipe pequena, isso pode ser literalmente "o fundador aprova e revisa a cada 6 meses" — é uma resposta válida em nível `A`. Não precisa fingir um comitê. |
| 71/72 | Treinamento de conscientização / campanhas de phishing | Se não existe, é barato de criar informalmente: uma reunião trimestral de 30min sobre boas práticas já sustenta nível `A`. Baixo custo, alto retorno na planilha. |
| 73 | Onboarding/offboarding com revogação de acesso | **Metade disso já é verdade tecnicamente**: `/Auth/logout-all` revoga todos os refresh tokens de um usuário instantaneamente (`AuthController.cs:111-122`). Só falta formalizar o processo humano (quem aciona isso quando alguém sai). |
| 108 | Gestão de dispositivos corporativos (MDM/EDR) | Se a equipe usa notebooks pessoais sem MDM, responder em branco é mais seguro. Considerar ao menos senha de tela + disco criptografado (BitLocker/FileVault) como resposta de nível `A`. |
| 113 | Proteção do e-mail corporativo (DMARC/DKIM/SPF) | **Isso é barato e é código/config, na real** — ver `NET-02` na seção 3, pode sair do "jurídico" e virar task. |

### 1.4 Depende de decisão de infraestrutura (escolher provedor de nuvem primeiro)

| # | Pergunta | Depende de |
|---|---|---|
| 15/16 | Onde os dados são processados/armazenados; residência no Brasil | **Decidido (2026-09-18): Azure, região `Brazil South`.** Passo a passo de execução em `migracao-azure-brasil.md` — só executar na assinatura do contrato. Itens 34, 62, 110, 111 seguem essa decisão. |
| 110/111 | CSPM / CWPP na nuvem pública | Com Azure já decidido: Azure Defender for Cloud tem tier gratuito/barato que já responde isso com nível `A`/`B` sem esforço de engenharia. |

### 1.5 Confirmação factual (não é decisão, é checar e responder)

| # | Pergunta | Ação |
|---|---|---|
| 50 | Incidente de segurança nos últimos 24 meses? | Confirme com o time: até onde investiguei no código/repos não há registro de incidente. Se realmente não houve, resposta é `D` ("nenhum incidente") — um dos poucos itens "de graça" na planilha. |

---

## 2. ⚠️ Antes de tudo: dois achados de segurança real (não são itens do formulário, são bugs)

Reforçando o que já te falei — **isso não espera priorização de Jira, faz hoje**:

1. **Chave privada do Firebase Admin SDK commitada** no repo mobile (`truckflow-driver-firebase-adminsdk-fbsvc-8edc18031c.json`, commit `062ca0e`). Revogar no console do Firebase → gerar nova → remover do histórico do git (`git filter-repo` ou BFG) → confirmar que o `.gitignore` cobre o padrão `*-firebase-adminsdk-*.json` (já cobre, só não retroativo).
2. **Senha do Postgres em texto plano** no `docker-compose.yml` do backend (`POSTGRES_PASSWORD: Nesher#2019`). Trocar por `${POSTGRES_PASSWORD}` lido de `.env` (gitignored) ou variável de ambiente do host.

Vou incluir esses dois como `SEC-01` e `SEC-02` no backlog abaixo só para efeito de rastreamento no Jira, mas trate como **hoje**, não como sprint planejada.

---

## 3. Backlog técnico — pronto para Jira

Convenção de prioridade: **P0** = bloqueia o piloto Aurora ou é risco de segurança ativo · **P1** = pré-requisito de rollout (já estava no ADR-0004) · **P2** = melhora nota do SAD e é razoavelmente barato · **P3** = nice-to-have / depende de decisão externa primeiro.

Cada task lista **em qual item(ns) do SAD ela mexe** e **para qual nível ela empurra a resposta** — assim você preenche a planilha à medida que fecha cada uma.

### Épico SEC — Segurança urgente

#### `SEC-01` — Rotacionar e remover a chave do Firebase Admin SDK do histórico do git
- **Prioridade:** P0 · **Repo:** mobile · **SAD:** achado fora do questionário, mas referenciado no item 25 (gestão de credenciais)
- **Onde:** `truckflow-driver-firebase-adminsdk-fbsvc-8edc18031c.json` (raiz do repo mobile), commit `062ca0e`
- **Como implementar:**
  1. No console do Firebase (Project Settings → Service Accounts), gerar nova chave e **revogar a antiga**.
  2. Remover o arquivo do working tree: `git rm truckflow-driver-firebase-adminsdk-*.json`.
  3. Limpar do histórico com `git filter-repo --path truckflow-driver-firebase-adminsdk-fbsvc-8edc18031c.json --invert-paths` (ou BFG Repo-Cleaner), depois force-push coordenado com o time (avisar antes — reescreve histórico).
  4. Nova chave vai para variável de ambiente / secret do serviço que a consome (provavelmente um backend function ou o próprio TruckFlow backend, se for lá que fica o Admin SDK).
- **Critério de aceite:** `git log --all --full-history -- '*firebase-adminsdk*'` não retorna nenhum blob com a chave antiga; nova chave nunca commitada.
- **Esforço:** ~1h (+ coordenação de force-push).

#### `SEC-02` — Tirar a senha do Postgres do `docker-compose.yml`
- **Prioridade:** P0 · **Repo:** backend · **SAD:** item 25 (gestão de credenciais)
- **Onde:** `docker-compose.yml:9`
- **Como implementar:** trocar `POSTGRES_PASSWORD: Nesher#2019` por `POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}`, criar `.env` local (gitignorado — já existe `.gitignore`, só confirmar padrão `.env`) com o valor real, documentar no `README.md` ao lado da seção de secrets que já existe.
- **Critério de aceite:** `docker-compose.yml` sem nenhum literal sensível; `git log -p -- docker-compose.yml` mostra a mudança mas a senha antiga já deve ser trocada por segurança (ela vazou).
- **Esforço:** 15min.

#### `SEC-03` — Parar de vazar stack trace em respostas HTTP 500
- **Prioridade:** P0 · **Repo:** backend · **SAD:** item 37 (tratamento de exceções)
- **Onde:** `src/TruckFlowApi/TruckFlow/Middlewares/ExceptionHandlingMiddleware.cs:66-78`
- **Como implementar:**
  ```csharp
  catch (Exception ex)
  {
      _logger.LogError("Erro inesperado: {exception}", ex.ToString());
      context.Response.StatusCode = 500;
      var body = _env.IsDevelopment()
          ? new { success = false, message = ex.Message, stack = ex.StackTrace }
          : new { success = false, message = "Erro interno. Tente novamente ou contate o suporte." };
      await context.Response.WriteAsJsonAsync(body);
  }
  ```
  Precisa injetar `IWebHostEnvironment _env` no construtor do middleware (mesmo padrão já usado em `AuthController.cs`).
- **Critério de aceite:** requisição forçando erro 500 em ambiente `Production`/`Staging` não retorna `stack` nem `ex.Message` cru no corpo.
- **Nível SAD:** item 37 sai de branco para `B` (tratamento estruturado, mensagens sanitizadas, log interno detalhado).
- **Esforço:** 30min.

---

### Épico IAM — Identidade, sessão e acesso

#### `IAM-01` — Rate limiting no endpoint de posição e no login
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 38 (segurança de APIs), item 27 (controles granulares)
- **Onde:** `Program.cs` (pipeline de middlewares), endpoints `POST /v1/motorista/posicao` e `POST /v1/Auth*/login`
- **Como implementar:** usar `Microsoft.AspNetCore.RateLimiting` (nativo do .NET 8, sem pacote externo):
  ```csharp
  builder.Services.AddRateLimiter(options => {
      options.AddFixedWindowLimiter("posicao", o => { o.Window = TimeSpan.FromSeconds(5); o.PermitLimit = 1; })
             .AddFixedWindowLimiter("login", o => { o.Window = TimeSpan.FromMinutes(1); o.PermitLimit = 5; });
  });
  // depois de UseAuthorization():
  app.UseRateLimiter();
  ```
  Aplicar `[EnableRateLimiting("posicao")]` no controller de localização e `[EnableRateLimiting("login")]` nos controllers de auth. Particionar por `MotoristaId`/IP (`RateLimitPartition.GetFixedWindowLimiter`).
- **Critério de aceite:** 2 requisições de posição do mesmo motorista em <5s → segunda retorna 429; 6 tentativas de login em 1min do mesmo IP → 429.
- **Nível SAD:** item 38 sobe de `A` para `B`/`C`.
- **Esforço:** 1 dia (já estava em ADR-0003 como decisão, só não implementado).

#### `IAM-02` — MFA (TOTP) para usuários Admin/Operador
- **Prioridade:** P1 · **Repo:** backend + admin · **SAD:** item 22 (MFA/SSO)
- **Onde:** backend usa `AspNetCore.Identity`, que já tem `AddDefaultTokenProviders()` (`Program.cs:140`) — o suporte a TOTP já está parcialmente presente no framework.
- **Como implementar:**
  1. Backend: expor endpoints `POST /v1/Auth/2fa/enable` (gera QR code via `UserManager.GenerateTwoFactorTokenAsync`/`GetAuthenticatorKeyAsync`) e `POST /v1/Auth/2fa/verify`.
  2. Fluxo de login passa a checar `TwoFactorEnabled` e, se ativo, exigir um segundo passo antes de emitir o JWT.
  3. Frontend admin: tela de configuração (mostrar QR code, ex.: biblioteca `qrcode` no Vue) + campo de código no login quando o backend sinalizar `requires2fa: true`.
  4. Começar **opcional por usuário**, não obrigatório — reduz atrito, ainda conta como "suporta MFA" na planilha.
- **Critério de aceite:** usuário com 2FA ativo não consegue logar só com senha; QR code funciciona com Google Authenticator/Authy.
- **Nível SAD:** item 22 sai de branco para `B` (MFA básico sem SSO).
- **Esforço:** 3 dias (backend 1,5d + frontend 1,5d).

#### `IAM-03` — Timeout de inatividade (auto-logout) no admin web e no mobile
- **Prioridade:** P2 · **Repo:** admin + mobile · **SAD:** item 30 (sessões e timeout)
- **Onde:** admin — `src/stores/AuthStore.ts` (ou equivalente) + um listener global de atividade; mobile — `src/stores/useAuthStore.ts`
- **Como implementar:** um hook/composable que reseta um timer a cada evento de interação (`mousemove`, `keydown`, `touchstart`) e, após N minutos sem atividade (sugestão: 20min admin, 60min mobile — motorista não deve ser deslogado no meio de uma viagem por ficar com o app em background), chama o fluxo de logout e redireciona para tela de login.
- **Critério de aceite:** sessão aberta e sem interação por N minutos força novo login; interação contínua não desloga.
- **Nível SAD:** item 30 sobe de `A` para `B`/`C`.
- **Esforço:** 1 dia (ambos os apps, é o mesmo padrão duplicado).

#### `IAM-04` — Relatório de recertificação de acessos
- **Prioridade:** P2 · **Repo:** backend + admin · **SAD:** item 21 (recertificação periódica)
- **Onde:** novo endpoint `GET /v1/admin/usuarios/relatorio-acesso` retornando usuário, papel, empresa, data do último login, data de criação; tela simples no admin listando isso com filtro por empresa.
- **Como implementar:** consulta em `Usuario` + `RefreshToken` (última emissão = proxy de último login) agrupada por `EmpresaId`. Não precisa de processo formal ainda — só a **ferramenta** que sustenta um processo trimestral manual.
- **Critério de aceite:** admin consegue exportar CSV com todos os usuários ativos da própria empresa e a data do último acesso.
- **Nível SAD:** item 21 sai de branco para `A` (revisão possível, ainda não é processo formal agendado).
- **Esforço:** 1 dia.

#### `IAM-05` — Endurecer política de senha
- **Prioridade:** P2 (quick win) · **Repo:** backend · **SAD:** item 24
- **Onde:** `Program.cs:135-140` (`AddIdentity<Usuario, IdentityRole<Guid>>`)
- **Como implementar:**
  ```csharp
  builder.Services.AddIdentity<Usuario, IdentityRole<Guid>>(options =>
  {
      options.User.RequireUniqueEmail = true;
      options.Password.RequiredLength = 10;
      options.Password.RequireNonAlphanumeric = true;
      options.Lockout.MaxFailedAccessAttempts = 5;
      options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
  })
  ```
  Documentar a política resultante num `SECURITY.md` curto (1 página) — vira a "evidência" pedida na coluna E do item 24.
- **Critério de aceite:** criação de usuário com senha de 6 caracteres é rejeitada; 5 tentativas falhas bloqueiam a conta por 15min.
- **Nível SAD:** item 24 sobe de `A` para `B`.
- **Esforço:** 2h.

#### `IAM-06` — SSO com provedor de identidade externo (Azure AD/Okta/Google Workspace)
- **Prioridade:** P3 · **Repo:** backend + admin · **SAD:** item 23 (provedores de identidade suportados)
- **Onde:** `TruckFlow/Extensions/Auth/AuthInjection.cs` (novo provedor externo além do `AspNetCore.Identity` interno); tela de login do admin.
- **Como implementar:** adicionar suporte a login via SAML/OAuth com um provedor popular (ex.: `Microsoft.Identity.Web` para Azure AD), mantendo o login usuário+senha atual como alternativa. Escopo inicial: só o admin web (motorista não precisa de SSO corporativo).
- **Critério de aceite:** usuário Admin consegue logar via conta corporativa do provedor configurado, sem precisar de senha própria no TruckFlow.
- **Nível SAD:** item 23 sai de branco para `A`/`B`.
- **Esforço:** 3-4 dias (maior esforço do backlog extra — escopo grande, avaliar se vale a pena antes do pedido explícito da Aurora).

#### `IAM-07` — Controles de acesso granulares (IP, geolocalização, horário, limite de sessões)
- **Prioridade:** P3 · **Repo:** backend · **SAD:** item 27 (controles granulares — não confundir com rate limiting do `IAM-01`)
- **Onde:** novo middleware/filtro de autorização, além do `[Authorize(Roles=...)]` já existente.
- **Como implementar:** allowlist de IP configurável por empresa (`EmpresaId`), limite de sessões simultâneas por usuário (invalidar refresh token mais antigo ao exceder), e restrição de horário de acesso configurável pelo admin da empresa.
- **Critério de aceite:** login fora do IP/horário permitido é bloqueado; sessão excedente força logout da mais antiga.
- **Nível SAD:** item 27 sai de branco para `A`/`B`.
- **Esforço:** 2 dias.

---

### Épico CI — Pipeline, SAST e qualidade

#### `CI-01` — Pipeline de CI no GitHub Actions (build + test) para os 3 repos
- **Prioridade:** P1 · **Repo:** backend, mobile, admin · **SAD:** item 32 (SAST/DAST), item 31 (SSDLC), item 11 (SCA de dependências)
- **Onde:** criar `.github/workflows/ci.yml` em cada repo (nenhum existe hoje).
- **Como implementar (backend):**
  ```yaml
  name: CI
  on: [pull_request]
  jobs:
    build-test:
      runs-on: ubuntu-latest
      steps:
        - uses: actions/checkout@v4
        - uses: actions/setup-dotnet@v4
          with: { dotnet-version: '8.0.x' }
        - run: dotnet restore
        - run: dotnet build --no-restore -c Release
        - run: dotnet test --no-build -c Release
  ```
  Adicionar depois: `dotnet format --verify-no-changes` (lint) e `dependabot.yml` (SCA gratuito, cobre item 11 sem esforço adicional).
- **Como implementar (mobile/admin — Node):**
  ```yaml
  - uses: actions/setup-node@v4
    with: { node-version: 20 }
  - run: npm ci
  - run: npm run build   # e npm test se/quando existir
  - run: npm audit --audit-level=high
  ```
- **Critério de aceite:** PR aberto contra `master`/`main` roda o workflow e bloqueia merge se build ou teste falhar (configurar branch protection depois).
- **Nível SAD:** item 32 sai de branco para `B` (CI com testes, sem SAST dedicado ainda); item 11 sai de branco para `A` (Dependabot cobre parte do SCA).
- **Esforço:** 1 dia total pros 3 repos (ADR-0004 já estimava 1d só pro backend).

#### `CI-02` — SAST leve: `dotnet format` + CodeQL / Semgrep gratuito
- **Prioridade:** P2 · **Repo:** backend (prioridade), depois admin/mobile · **SAD:** item 32, item 33
- **Onde:** extensão do `ci.yml` do `CI-01`
- **Como implementar:** adicionar o workflow padrão do GitHub CodeQL (`github/codeql-action`, gratuito para repositórios) ou Semgrep CLI (`semgrep ci`, free tier). Roda em paralelo ao build, não bloqueia merge inicialmente (modo report-only) até o time se acostumar com o volume de findings.
- **Critério de aceite:** dashboard de "Security" do GitHub passa a listar findings do CodeQL.
- **Nível SAD:** item 32 sobe de `B` para `C`.
- **Esforço:** meio dia.

---

### Épico TEST — Testes de integração e multi-tenant (ADR-0004, item 14)

#### `TEST-01` — Setup do `DatabaseFixture` + `TenantFixture` no `TruckFlow.Test`
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 78 (auditoria/validação independente de segregação de tenant), item 31
- **Onde:** `TruckFlow.Test/` (hoje só tem testes unitários de domínio/validators — bom, mas sem integração real de banco)
- **Como implementar:** exatamente o plano já detalhado no `Docs/adr/0004-prerequisitos-rollout-aurora.md` (seção "Item 14"):
  1. `ProjectReference` para `Domain`, `Application`, `Infra`, API.
  2. Trocar `SqlServer` por `Npgsql.EntityFrameworkCore.PostgreSQL` no `.csproj` de teste.
  3. `DatabaseFixture : IAsyncLifetime` com Testcontainers (recomendado — evita depender de Postgres local) ou banco `TruckFlow_test` dedicado.
  4. `TenantFixture` que registra um `IEmpresaContext` fake por teste.
- **Critério de aceite:** `dotnet test` sobe um Postgres efêmero, roda migrations, e os testes abaixo (`TEST-02`) passam isolados.
- **Esforço:** 1 dia (conforme ADR-0004).

#### `TEST-02` — Casos de teste: vazamento cross-tenant, concorrência, dispatcher
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 78, item 20, item 77 (reforça a evidência do isolamento já forte)
- **Onde:** novos arquivos em `TruckFlow.Test/Integration/`
- **Como implementar:** os 4 casos já especificados no ADR-0004:
  1. `Reservar` com 100 threads concorrentes no mesmo slot → 1 sucesso, 99 falhas de concorrência.
  2. Empresa A cancela agendamento → notificação criada com `EmpresaId=A`; usuário de B chamando `ListarMinhasAsync()` recebe lista vazia; `MarcarComoLidaAsync` de B numa notificação de A retorna `false`/404.
  3. Transições de status do `Agendamento` (todas combinações válidas/inválidas).
  4. Dispatcher com `FOR UPDATE SKIP LOCKED`: 2 workers concorrentes não processam o mesmo evento duas vezes.
- **Critério de aceite:** suite verde, roda no `CI-01`.
- **Nível SAD:** item 78 sai de branco para `B` ("testes automatizados internos", ainda não é auditoria de terceiro — isso é `D`).
- **Esforço:** 2 dias (conforme ADR-0004).

---

### Épico INFRA — Continuidade, backup e disponibilidade

#### `INFRA-01` — Backup automatizado (`pg_dump` diário + WAL archiving)
- **Prioridade:** P0 (maior gap isolado da planilha) · **Repo:** backend/infra · **SAD:** itens 60, 61, 62, 59
- **Onde:** depende do provedor escolhido (ver seção 1.4). Se for Postgres gerenciado (Aiven/RDS/Supabase — recomendado no ADR-0004), backup automático + PITR já vem pronto, só precisa **configurar retenção e confirmar**.
- **Como implementar (se self-hosted):**
  1. Cron job diário: `pg_dump -Fc` para bucket externo (S3/Backblaze/Azure Blob) com lifecycle policy de retenção (ex.: 30 dias).
  2. `archive_command` no `postgresql.conf` apontando pro mesmo bucket → habilita PITR.
  3. Job mensal de teste de restore (`pg_restore` num banco descartável) — é o que responde o item 61.
- **Critério de aceite:** existe backup de ontem recuperável; teste de restore documentado com data e resultado.
- **Nível SAD:** itens 60/61/62 saem de branco para `B`/`C`.
- **Esforço:** 1 dia se usar Postgres gerenciado (só configuração); 2-3 dias se self-hosted.

#### `INFRA-02` — Health check endpoint + `HEALTHCHECK` no Dockerfile
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 83 (monitoramento), item 75 (SLA)
- **Onde:** `Program.cs` (adicionar `MapHealthChecks`), `src/TruckFlowApi/TruckFlow/Dockerfile:4` (falta `HEALTHCHECK`)
- **Como implementar:**
  ```csharp
  builder.Services.AddHealthChecks()
      .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);
  // ...
  app.MapHealthChecks("/health");
  ```
  ```dockerfile
  HEALTHCHECK --interval=30s --timeout=5s CMD curl -f http://localhost:8080/health || exit 1
  ```
- **Critério de aceite:** `GET /health` retorna 200 com banco up, 503 com banco down; `docker ps` mostra status `healthy`.
- **Esforço:** meio dia (conforme ADR-0004, item 3).

#### `INFRA-03` — Migrations fora do `Program.cs` (job separado no deploy)
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 66 (segregação de ambientes), pré-requisito técnico do `INFRA-04`
- **Onde:** `Program.cs:157-163` (`db.Database.Migrate()` no boot)
- **Como implementar:** criar um comando CLI separado (`dotnet TruckFlow.dll --migrate-only` usando `if (args.Contains("--migrate-only")) { db.Database.Migrate(); return; }`) ou um projeto console dedicado. No pipeline de deploy, rodar esse comando **antes** de subir a nova versão da API.
- **Critério de aceite:** 2 instâncias da API subindo ao mesmo tempo não competem por `__EFMigrationsHistory`.
- **Esforço:** meio dia (conforme ADR-0004, item 2).

#### `INFRA-04` — Multi-instância + reverse proxy (nginx/Traefik)
- **Prioridade:** P1 · **Repo:** backend/infra · **SAD:** item 69 (alta disponibilidade), item 79 (escalabilidade)
- **Onde:** infra de deploy (fora do código da aplicação)
- **Como implementar:** 2+ instâncias da API atrás de nginx/Traefik, sem sticky session (API já é stateless — SSE reconecta sozinho). Depende de `INFRA-03` estar pronto primeiro (senão as instâncias competem nas migrations).
- **Critério de aceite:** derrubar uma instância não gera downtime perceptível; deploy rolling sem janela de manutenção.
- **Nível SAD:** item 69 sai de branco para `B`/`C`.
- **Esforço:** 2-3 dias (conforme ADR-0004, item 1).

#### `INFRA-05` — Distributed lock no `AgendamentoExpirationService`
- **Prioridade:** P1 (só relevante depois do `INFRA-04`) · **Repo:** backend · **SAD:** reforça item 69/79
- **Onde:** o `IHostedService` de expiração de agendamento
- **Como implementar:** mesmo padrão do `NotificacaoDispatcherService` (ADR-0002) — `SELECT ... FOR UPDATE SKIP LOCKED` na query de expiração, pra 2 instâncias não tentarem expirar o mesmo agendamento.
- **Esforço:** meio dia (conforme ADR-0004, item 4).

#### `INFRA-06` — Ambiente de staging/homologação
- **Prioridade:** P2 · **Repo:** infra (todos) · **SAD:** item 66, item 86
- **Onde:** infra de deploy
- **Como implementar:** segunda instância do backend + admin apontando pra um banco `TruckFlow_staging` separado, com dados sintéticos (não cópia de produção — evita precisar de mascaramento agora). Deploy automático em cada merge na branch `develop`/`staging`.
- **Nível SAD:** item 66 sai de branco para `B`; item 86 sai de branco para `A`.
- **Esforço:** 1 dia (majoritariamente config de infra).

#### `INFRA-07` — Controles de acesso entre ambientes + replicação com mascaramento
- **Prioridade:** P3 · **Repo:** infra (todos) · **SAD:** item 67 (controles de acesso entre ambientes), item 68 (replicação com mascaramento)
- **Onde:** extensão do `INFRA-06` — credenciais/acesso ao ambiente de staging.
- **Como implementar:** usuários/roles distintos por ambiente (dev/staging/prod) no provedor de hospedagem, sem compartilhar credenciais; se algum dado real precisar ser copiado pra staging no futuro, aplicar mascaramento de campos sensíveis (CPF, telefone) no processo de cópia — hoje o `INFRA-06` já evita isso usando dados sintéticos.
- **Critério de aceite:** acesso ao staging não usa as mesmas credenciais/permissões do ambiente de produção.
- **Nível SAD:** item 67 sai de branco para `B`; item 68 segue "não aplicável" (sem processo de replicação de dados reais).
- **Esforço:** meio dia (depende do `INFRA-06` já existir).

#### `INFRA-08` — Ativar Cloudflare (proxy + WAF managed ruleset) + regras customizadas
- **Prioridade:** P1 · **Repo:** infra (DNS) · **SAD:** itens 52, 57, 106 (WAF, threat intel implícito no ruleset, DDoS)
- **Onde:** configuração de DNS do domínio (fora do repo) + painel Cloudflare.
- **Como implementar:** ao apontar o domínio, ativar o proxy (nuvem laranja) e habilitar o Managed Ruleset gratuito em Security > WAF. Depois, criar 2-3 regras customizadas próprias (ex.: bloquear padrões específicos relevantes ao domínio) além do ruleset padrão — o plano free permite algumas regras customizadas.
- **Critério de aceite:** domínio proxied pelo Cloudflare, WAF managed ruleset ativo, pelo menos 2 regras customizadas configuradas.
- **Nível SAD:** item 52 sai de branco para `C`; item 106 sai de branco para `C` (DDoS via CDN/WAF).
- **Esforço:** poucas horas.

#### `INFRA-09` — Object Lock (WORM) no bucket de backup
- **Prioridade:** P1 · **Repo:** infra · **SAD:** item 62 (imutabilidade de backup)
- **Onde:** configuração do bucket de armazenamento externo usado pelo `INFRA-01` (backup).
- **Como implementar:** habilitar a opção de Object Lock / bloqueio de objeto (WORM) nas configurações do bucket, definindo o período de retenção imutável.
- **Critério de aceite:** objetos de backup não podem ser sobrescritos ou excluídos dentro do período de retenção configurado.
- **Nível SAD:** item 62 sai de `A` para `C`.
- **Esforço:** poucos minutos (configuração), depende do `INFRA-01` já existir.

---

### Épico OBS — Observabilidade e logs

#### `OBS-01` — OpenTelemetry → Prometheus/Grafana
- **Prioridade:** P2 · **Repo:** backend · **SAD:** itens 82, 83 (desempenho monitorado)
- **Onde:** `Program.cs` (novo `AddOpenTelemetry()`)
- **Como implementar:** pacotes `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.EntityFrameworkCore`, `OpenTelemetry.Exporter.Prometheus.AspNetCore`. Métricas mínimas: latência de `Reservar`, taxa de cancelamento, lag do dispatcher, conexões PG em uso. Grafana Cloud free tier cobre o volume do piloto.
- **Nível SAD:** itens 82/83 saem de branco para `B`.
- **Esforço:** 2 dias (conforme ADR-0004, item 9).

#### `OBS-02` — Logs centralizados (Seq self-hosted ou Grafana Loki)
- **Prioridade:** P2 · **Repo:** backend · **SAD:** item 54 (retenção/exportação de logs)
- **Onde:** `appsettings.json` (seção `Serilog:WriteTo`, hoje só Console + File)
- **Como implementar:** adicionar sink `Serilog.Sinks.Seq` (mais simples de subir via Docker) ou `Serilog.Sinks.Grafana.Loki`. Definir política de retenção (ex.: 90 dias) direto na configuração do Seq/Loki.
- **Critério de aceite:** logs de todas as instâncias aparecem centralizados, buscáveis por `EmpresaId`/`UserId`.
- **Nível SAD:** item 54 sobe de `A` para `B`/`C`; abre caminho pra exportação em formato Syslog se a Aurora pedir.
- **Esforço:** 1 dia (conforme ADR-0004, item 10).

#### `OBS-03` — Mascaramento de dados sensíveis em logs
- **Prioridade:** P2 · **Repo:** backend · **SAD:** item 36
- **Onde:** configuração do Serilog (`Program.cs` ou `appsettings.json`) + DTOs que trafegam CPF/telefone/e-mail
- **Como implementar:** usar `Serilog.Enrichers` com uma política de destructuring custom que mascara campos marcados com um atributo `[SensitiveData]` (CPF, telefone, e-mail parcial) antes de logar. Alternativa mais simples: revisar os `_logger.LogInformation` existentes e garantir que nenhum loga o objeto completo de `Usuario`/`Motorista` (só IDs).
- **Nível SAD:** item 36 sai de branco para `A`/`B`.
- **Esforço:** 1 dia.

#### `OBS-04` — Alertas de segurança automáticos ao cliente (login de IP novo, mudança de permissão)
- **Prioridade:** P3 · **Repo:** backend + admin · **SAD:** item 55 (alertas automáticos), item 56 (alertas configuráveis pelo cliente)
- **Onde:** novo listener em cima do `AuditSaveChangesInterceptor` já existente + `INotificacaoService` (já usado para notificações in-app/push).
- **Como implementar:** disparar uma notificação (in-app + e-mail) para os Admins da empresa quando: (1) login administrativo de um IP não visto nos últimos 30 dias, (2) alteração de papel/permissão de um usuário. Fase 2 (fora do escopo inicial): tela de preferências pra empresa escolher quais eventos quer ser alertada.
- **Critério de aceite:** login de admin de IP novo gera notificação visível na tela de auditoria em até 1 minuto.
- **Nível SAD:** item 55 sai de branco para `A`/`B`; item 56 segue em branco (configuração pelo cliente fica pra fase 2).
- **Esforço:** 1,5 dia.

#### `OBS-05` — Logs de aplicação centralizados no Grafana + revisão mensal
- **Prioridade:** P1 · **Repo:** backend · **SAD:** item 53 (registro centralizado de eventos de segurança)
- **Onde:** `appsettings.json` (seção `Serilog:WriteTo`) — extensão do `OBS-01`/`OBS-02` que já leva métricas/logs pro Grafana.
- **Como implementar:** adicionar sink de logs (ex.: Grafana Loki) além do que já existe (Console/File/Seq), garantindo que os logs de segurança da aplicação (não só eventos de WAF) fiquem no mesmo lugar. Agendar (calendário/lembrete) revisão mensal desses logs pelo responsável técnico, conforme já declarado na política de segurança.
- **Critério de aceite:** logs de aplicação buscáveis no mesmo painel usado pros alertas do WAF; revisão mensal registrada.
- **Nível SAD:** item 53 sobe de `B` para `C`.
- **Esforço:** meio dia.

#### `OBS-06` — Alertas de performance/latência no Grafana
- **Prioridade:** P2 · **Repo:** backend · **SAD:** item 83 (monitoramento com alertas)
- **Onde:** dashboards do `OBS-01` no Grafana.
- **Como implementar:** configurar regras de alerta (mesmo mecanismo de notificação já usado pro WAF no `INFRA-08`) para desvios de latência nas transações críticas (ex.: `Reservar`).
- **Critério de aceite:** alerta disparado quando a latência de uma transação crítica ultrapassa o limite definido.
- **Nível SAD:** item 83 sobe de `C` (dashboard) para `C` confirmado (dashboard + alerta).
- **Esforço:** poucas horas, depende do `OBS-01` e `INFRA-08` já existirem.

---

### Épico CRYPTO — Chaves e criptografia

#### `CRYPTO-01` — Mover JWT signing key para um vault gerenciado + rotação
- **Prioridade:** P2 (P1 se o provedor de nuvem escolhido já tiver Key Vault de graça) · **Repo:** backend/infra · **SAD:** item 35
- **Onde:** hoje a chave vem de `dotnet user-secrets` (dev) / variável de ambiente (prod) — `appsettings.json:12` está vazio corretamente, mas não há rotação.
- **Como implementar:** se for Azure, usar Azure Key Vault + `Azure.Extensions.AspNetCore.Configuration.Secrets` (poucas linhas de config). Se AWS, Secrets Manager. Documentar um procedimento manual de rotação trimestral (script que gera nova chave, atualiza o vault, reinicia a API com zero-downtime graças ao `INFRA-04`).
- **Nível SAD:** item 35 sai de branco para `B`.
- **Esforço:** 1 dia (depende de já ter escolhido o provedor).

#### `CRYPTO-02` — Criptografia em nível de coluna para campos sensíveis específicos
- **Prioridade:** P3 · **Repo:** backend · **SAD:** item 34 (padrões de criptografia em trânsito e em repouso — complementa o HTTPS já existente)
- **Onde:** `AppDbContext.cs` (configuração do EF Core) para colunas como CPF/telefone em `Motorista`, `Usuario`.
- **Como implementar:** usar um `ValueConverter` do EF Core (`HasConversion`) com criptografia simétrica (AES) nas colunas mais sensíveis, com a chave vinda do mesmo mecanismo do `CRYPTO-01`. Aumenta a complexidade de queries que filtram por esses campos (deixam de ser indexáveis diretamente) — avaliar impacto antes de implementar.
- **Critério de aceite:** dump direto do banco não expõe CPF/telefone em texto plano.
- **Nível SAD:** item 34 sobe de branco para `B`/`C` (a critério de quão bem isso se encaixa no restante da nota, já que TLS via HTTPS já cobre "em trânsito").
- **Esforço:** 2 dias (inclui migração de dados existentes).

---

### Épico MOB — Mobile (app do motorista)

#### `MOB-01` — Fila offline com `expo-sqlite` para captura de localização
- **Prioridade:** P1 · **Repo:** mobile · **SAD:** item 91 (funcionalidade offline)
- **Onde:** o ADR `docs/adr/0002-captura-localizacao.md` do próprio repo mobile já especifica o design — só não foi implementado. Nenhuma dependência de SQLite/location/task-manager está instalada hoje.
- **Como implementar:**
  1. `npx expo install expo-sqlite expo-location expo-task-manager`.
  2. Tabela local `posicoes_pendentes` (lat, lng, accuracy, capturadoEm, agendamentoId).
  3. Ao falhar o `POST /v1/motorista/posicao` por falta de rede, gravar local em vez de descartar.
  4. Listener de reconexão (`@react-native-community/netinfo` ou `expo-network`) dispara flush do buffer em lote pro endpoint (que já é batch-first, conforme ADR-0003 do backend).
- **Critério de aceite:** app em modo avião acumula posições; ao reconectar, todas são enviadas em um único `POST` batch.
- **Nível SAD:** item 91 sai de branco para `B`/`C`.
- **Esforço:** 3 dias.

#### `MOB-02` — Tela de consentimento LGPD + preferências de rastreamento
- **Prioridade:** P1 · **Repo:** mobile · **SAD:** item 9 (LGPD), pré-requisito para captura de localização em produção conforme ADR-0003 do backend
- **Onde:** `docs/adr/0002-captura-localizacao.md` (mobile) já especifica o texto do modal e a tela `app/(app)/usuario/perfil/privacidade.tsx` — arquivo ainda não existe.
- **Como implementar:** modal full-screen no primeiro uso ("Sua localização será compartilhada com a fábrica X enquanto o agendamento estiver ativo. Você pode revogar isso a qualquer momento.") com botões Aceitar/Recusar; tela de preferências com toggle persistido (via `expo-secure-store` ou backend); toggle default `false` até aceite explícito.
- **Critério de aceite:** app não envia nenhum ping de localização antes do consentimento explícito; revogar o toggle para o envio imediatamente.
- **Nível SAD:** item 9 sobe de `A` para `B`/`C`.
- **Esforço:** 2 dias.

#### `MOB-03` — Acessibilidade básica (labels)
- **Prioridade:** P3 · **Repo:** mobile · **SAD:** item 90
- **Onde:** todos os componentes interativos em `src/`/`app/` — zero ocorrências de `accessibilityLabel`/`accessibilityRole` hoje.
- **Como implementar:** passar uma primeira rodada adicionando `accessibilityLabel` e `accessibilityRole` nos componentes de navegação principal e botões de ação crítica (agendar, confirmar chegada, enviar NF). Não precisa cobrir 100% da app pra sair do zero.
- **Nível SAD:** item 90 sai de branco para `A`.
- **Esforço:** 1-2 dias (cobertura parcial, priorizando fluxo crítico).

#### `MOB-04` — Desregistrar push token no logout
- **Prioridade:** P3 · **Repo:** mobile + backend · **SAD:** reforça item 25/28 (ciclo de vida de credenciais)
- **Onde:** `src/stores/useAuthStore.ts` (`signOut`) hoje só chama `/Auth/logout-mobile`, não desregistra o device.
- **Como implementar:** adicionar chamada a um endpoint `DELETE /v1/dispositivo` (verificar se já existe endpoint equivalente no backend, conforme `ADR-0001-integracao-push-notifications.md` do mobile) antes/depois do logout, removendo o token do banco de dispositivos.
- **Esforço:** meio dia.

---

### Épico WEB — Admin (frontend)

#### `WEB-01` — Idle timeout (ver `IAM-03`, mesma task, front admin)

#### `WEB-02` — Acessibilidade básica (aria-labels + lint)
- **Prioridade:** P3 · **Repo:** admin · **SAD:** item 90
- **Onde:** `src/` inteiro — zero `aria-*` hoje, sem `eslint-plugin-vuejs-accessibility` configurado.
- **Como implementar:** `npm i -D eslint-plugin-vuejs-accessibility`, habilitar no `eslint.config.*` (criar se não existir — hoje não há config de ESLint no projeto, o que também vale resolver por qualidade geral). Rodar `--fix` onde possível, revisar manualmente formulários e botões de ação principal.
- **Nível SAD:** item 90 sai de branco para `A`.
- **Esforço:** 1 dia inicial (config + primeira leva) + backlog contínuo.

#### `WEB-03` — Implementar a geração real de relatórios (hoje é mock)
- **Prioridade:** P2 · **Repo:** admin + backend · **SAD:** itens 92, 94
- **Onde:** `src/views/Relatorio.vue:265-278` — `gerarRelatorio()` hoje só faz `setTimeout` + `console.log`, sem chamada real de API nem geração de arquivo.
- **Como implementar:** endpoint backend `GET /v1/relatorios/agendamentos?formato=csv|pdf&filtros=...` retornando `FileStreamResult`; front troca o mock por chamada real + download do blob retornado (`URL.createObjectURL`).
- **Critério de aceite:** botão "Gerar relatório" produz um arquivo CSV/PDF real com os dados filtrados.
- **Nível SAD:** item 92 sobe de `A` para `B`; item 94 sai de branco para `A` (se incluir agendamento futuro, senão fica em `A` só pela exportação manual).
- **Esforço:** 2 dias.

#### `WEB-04` — CSP básica + headers de segurança
- **Prioridade:** P3 · **Repo:** admin · **SAD:** reforça item 38/43 (segurança geral de aplicação web)
- **Onde:** `vercel.json` (hoje só tem rewrite de SPA, sem headers)
- **Como implementar:** adicionar `headers` no `vercel.json`:
  ```json
  { "headers": [{ "source": "/(.*)", "headers": [
      { "key": "Content-Security-Policy", "value": "default-src 'self'; connect-src 'self' https://api.truckflow.app; img-src 'self' data:;" },
      { "key": "X-Content-Type-Options", "value": "nosniff" },
      { "key": "X-Frame-Options", "value": "DENY" }
  ]}]}
  ```
  Ajustar `connect-src`/`img-src` conforme domínios reais usados (Maps, API).
- **Esforço:** meio dia (+ ajustes finos depois de testar em produção).

#### `WEB-05` — Corrigir bugs pequenos encontrados na varredura (opcional, baixo risco)
- **Prioridade:** P3 · **Repo:** admin
- Checkbox "Lembrar-me" em `LoginView.vue:336-343` está declarado mas nunca lido — remover ou implementar.
- `VisualizarAgendamentoView.vue:810` usa `alert()` nativo em vez do componente de toast do resto do app — trocar por consistência de UX (não é achado de segurança, é papel cortado).
- **Esforço:** 1h no total.

#### `WEB-06` — Documentar matriz de navegadores/SO suportados
- **Prioridade:** P3 · **Repo:** admin + mobile · **SAD:** item 89 (navegadores/SOs suportados)
- **Onde:** `browserslist` no `package.json` do admin (não existe hoje); versão mínima de iOS/Android no `app.json` do mobile (hoje usa default do Expo SDK 54).
- **Como implementar:** definir e documentar explicitamente as versões suportadas (ex.: últimas 2 versões de Chrome/Safari/Edge; iOS 15+/Android 10+), em vez de depender implicitamente do default das ferramentas.
- **Critério de aceite:** `README.md` ou `SECURITY.md` lista navegadores/SOs suportados.
- **Nível SAD:** item 89 sai de branco para `B`.
- **Esforço:** 30min — é só documentação, não muda comportamento.

---

### Épico DATA — Portabilidade e eliminação de dados (LGPD)

#### `DATA-01` — Endpoint de exportação completa dos dados de uma empresa
- **Prioridade:** P2 · **Repo:** backend + admin · **SAD:** itens 96, 97, 98
- **Onde:** novo endpoint `GET /v1/admin/empresas/{id}/exportar-dados` (Admin-only, own tenant)
- **Como implementar:** job assíncrono que varre todas as entidades `IEmpresaScoped` da empresa, serializa em JSON (ou CSV por entidade, zipado), disponibiliza link de download temporário (expira em 24-48h). Reaproveita a mesma lista de entidades `IEmpresaScoped` que já é usada no `AppDbContext` pra registrar os query filters — não precisa mapear manualmente de novo.
- **Critério de aceite:** exportação gera um arquivo com todas as tabelas da empresa, sem vazar dados de outro tenant (mesma garantia do isolamento já existente).
- **Nível SAD:** item 96 sai de branco para `B`; item 97/98 saem de branco para `A`.
- **Esforço:** 2 dias.

#### `DATA-04` — Checksum de integridade no arquivo de exportação
- **Prioridade:** P2 · **Repo:** backend · **SAD:** item 98 (integridade do processo de extração)
- **Onde:** extensão do `DATA-01` — job de exportação.
- **Como implementar:** calcular hash (ex.: SHA-256) do arquivo gerado antes de disponibilizar o link de download, e exibir/entregar o hash junto (ex.: arquivo `.sha256` ao lado do export).
- **Critério de aceite:** todo arquivo exportado vem acompanhado do seu checksum.
- **Nível SAD:** item 98 sobe de `A` para `C`.
- **Esforço:** poucas horas, depende do `DATA-01` já existir.

#### `DATA-02` — Endpoint de exclusão completa dos dados de uma empresa (fim de contrato)
- **Prioridade:** P3 (menos urgente que `DATA-01` — só é acionado no offboarding de um cliente) · **Repo:** backend · **SAD:** itens 99, 100
- **Onde:** novo endpoint administrativo (não exposto ao cliente final, uso interno da TruckFlow) que deleta em cascata todos os registros `IEmpresaScoped` de uma empresa + backups relacionados.
- **Como implementar:** transação que percorre as mesmas entidades do `DATA-01`, deleta em ordem que respeite FKs, e grava um registro de auditoria do evento **sem os dados** (igual ao padrão já usado no `DELETE /v1/motorista/eu/historico-localizacao`, que serve de modelo pronto). Esse registro de auditoria é o que sustenta a "certificação" pedida no item 100.
- **Nível SAD:** item 99 sobe de `A` para `B`; item 100 sai de branco para `A`.
- **Esforço:** 1,5 dia.

#### `DATA-03` — Purge automático pós-contrato (após decisão jurídica do item 101)
- **Prioridade:** P3 · **Repo:** backend · **SAD:** item 101
- **Onde:** depende de `DATA-02` já existir; só falta um job agendado.
- **Como implementar:** campo `Empresa.ContratoEncerradoEm`; job diário (`IHostedService`, mesmo padrão do `AgendamentoExpirationService`) que dispara `DATA-02` automaticamente N dias depois (N = o que for definido juridicamente).
- **Esforço:** meio dia, depois que a cláusula jurídica existir.

---

### Épico NET — Proteção de rede e e-mail (baixo esforço, alto retorno na planilha)

#### `NET-01` — Habilitar proteção nativa de nuvem (CSPM) do provedor escolhido
- **Prioridade:** P2 · **Repo:** infra · **SAD:** itens 110, 111
- **Onde:** console do provedor de nuvem escolhido (ver seção 1.4)
- **Como implementar:** Azure → ativar **Microsoft Defender for Cloud** (tier gratuito cobre CSPM básico); AWS → **Security Hub** + **GuardDuty** (free tier); GCP → **Security Command Center** (tier padrão gratuito). É literalmente marcar uma opção no console, sem código.
- **Nível SAD:** itens 110/111 saem de branco para `A`/`B` **sem nenhum esforço de engenharia**, só decisão + 10 minutos de configuração.
- **Esforço:** 1h (depois de já ter escolhido o provedor).

#### `NET-02` — DMARC/DKIM/SPF no domínio de e-mail corporativo
- **Prioridade:** P2 (barato, some da lista jurídica) · **Repo:** infra/DNS · **SAD:** item 113
- **Onde:** registros DNS do domínio usado para e-mail corporativo (se houver — hoje o `Resend` do backend usa `onboarding@resend.dev`, domínio de teste; se a empresa tiver domínio próprio para e-mail transacional/corporativo, configurar lá).
- **Como implementar:** adicionar registros SPF (`v=spf1 include:_spf.resend.com ~all` ou equivalente do provedor de e-mail transacional), DKIM (gerado pelo provedor), e DMARC (`v=DMARC1; p=quarantine; rua=mailto:...`) no DNS.
- **Nível SAD:** item 113 sai de "jurídico/RH" pra resolvido tecnicamente em `B`/`C`.
- **Esforço:** 2h (se já tiver domínio próprio) — senão depende de decisão comercial de ter e-mail corporativo formal primeiro.

---

### Épico VULN — Gestão de vulnerabilidades

#### `VULN-01` — Processo formal de triagem de vulnerabilidades + SLA por criticidade
- **Prioridade:** P3 · **Repo:** backend (processo, não é código) · **SAD:** item 64 (processo de gestão de vulnerabilidades), item 65 (SLA por criticidade)
- **Onde:** documento novo, `Docs/VULN-MANAGEMENT.md` ou seção do `SECURITY.md` do `IAM-05`.
- **Como implementar:** o `CI-02` já gera findings via CodeQL no dashboard de Security do GitHub — falta só formalizar o processo: quem triagem os achados, com que frequência, e prazo de correção por severidade (sugestão: crítica em 7 dias, alta em 15, média em 30, baixa em 60 — mesmo padrão do nível `C` da planilha).
- **Critério de aceite:** documento publicado definindo dono do processo e SLA por severidade.
- **Nível SAD:** item 64 sai de branco para `B`; item 65 sai de branco para `C`.
- **Esforço:** meio dia (é processo/documentação, não código — mas depende do `CI-02` já existir pra gerar os achados).

---

## 4. Ordem sugerida de execução

1. **Hoje/amanhã:** `SEC-01`, `SEC-02`, `SEC-03` (achados de segurança ativos), `MOB-01` (fila offline + endpoint de posição — antecipado: revisão da Aurora é semana que vem, e o item 91 da planilha já assume essa funcionalidade pronta), `MOB-02` (LGPD mobile — exigência regulatória, não só nota de planilha), `DATA-01` (exportação completa de dados — antecipado: itens 96/97/98 assumem pronto), `DATA-02` (exclusão completa de dados — antecipado: itens 99/100 assumem pronto), `DATA-04` (checksum de exportação — item 98 assume pronto), `INFRA-08` (Cloudflare WAF+regras — itens 52/57/106 assumem pronto), `INFRA-09` (Object Lock backup — item 62 assume pronto), `OBS-05` (logs de app no Grafana + revisão mensal — item 53 assume pronto), `OBS-06` (alertas de performance no Grafana — item 83 assume pronto).
2. **Semana 1:** decisão de provedor de nuvem (seção 1.4) — desbloqueia `INFRA-01`, `NET-01`, itens 15/16/34/62/110/111 da planilha. Em paralelo: `IAM-05` (quick win), `CI-01` (pipeline base nos 3 repos).
3. **Semanas 2-3:** `INFRA-03` → `INFRA-04` → `INFRA-05` (nessa ordem, uma depende da outra), `INFRA-02`, `IAM-01`, `TEST-01` → `TEST-02`, `IAM-02` (MFA), `IAM-03`/`WEB-01`.
5. **Depois do piloto validar:** `OBS-01`, `OBS-02`, `OBS-03`, `WEB-03`.
6. **Backlog extra (P3, sem pressa):** `IAM-06`, `IAM-07`, `CRYPTO-02`, `OBS-04`, `INFRA-07`, `WEB-06`, `VULN-01` — identificados numa segunda varredura completa das 113 perguntas do SAD depois de fechar a Sprint 13; são "melhoria", não bloqueiam a saída da faixa "Crítica".

Isso cobre a maior parte do que hoje está em branco em GRC-técnico, IAM, AppSec, SecOps e Arquitetura — os pilares onde a concorrente também foi mais fraca (17-25%). Combinado com os itens já fortes (multi-tenant, refresh token), dá pra sair de "Crítica" com esforço concentrado de ~4-5 semanas, sem depender de nada jurídico/comercial pra isso.

---

*Gerado a partir de leitura de código dos 3 repos (`TruckFlow`, `tf-mobile`, `TruckFlowApp`) + comparação com `formularios_sad_181_00 (2).xlsx` (exemplo de concorrente avaliada pela Aurora) em 2026-08-22. Fonte complementar: `Docs/adr/0001` a `0004`.*
