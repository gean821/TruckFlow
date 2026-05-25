# ADR-0002 — Sistema de notificações: push + SSE in-app; WhatsApp via deep-link

- **Status**: Aceito (revisado em 2026-05-21 — ver seção "Revisão 2026-05-21")
- **Data**: 2026-05-14
- **Pendência aberta**: ver seção "Decisão pendente com cliente"

## Contexto

A operação Aurora exige notificação confiável em dois sentidos:

1. **Admin → motorista**: cancelamento, reagendamento, alerta de janela próxima ao fim.
2. **Motorista → admin**: "vou atrasar", "preciso cancelar", check-in/check-out (alguns eventos hoje, outros não).

Pelo dimensionamento de [ADR-0001](./0001-alvo-aurora.md), o volume é ~1.000-1.500 notificações/dia em regime. Pelo menos um canal precisa funcionar com tela do app bloqueada (motorista pode estar dirigindo).

Restrições adicionais que pesaram na escolha:

- WhatsApp Business Cloud API exige **verificação Meta Business da Aurora** (não do fornecedor) + **aprovação de templates por categoria de uso**, processo de 1-3 semanas + retrabalho a cada mudança de redação.
- Aurora opera hoje muito via WhatsApp informal — alguns admins já têm conversas pessoais com motoristas frequentes.
- LGPD exige consentimento informado por canal, opção de revogação, retenção definida.
- Multi-instance + 21 fábricas exige que real-time não dependa de estado em memória de uma instância.

## Decisão

### Canais ativos no dispatcher automático

**1. Push pro motorista — Expo Notifications**

- Lib mobile: `expo-notifications` + `expo-device`.
- Backend dispara via Expo Push API (HTTP, sem custo, sem configuração FCM/APNs manual).
- Mobile registra `expoPushToken` no backend em login e em cold start.
- Backend mantém `DispositivoUsuario` (1 usuário → N dispositivos). Token vindo da Expo como `DeviceNotRegistered` desativa o registro na hora.
- Worker secundário consulta receipts da Expo (até 24h após envio) e atualiza `NotificacaoEntrega.Status` para `Enviado` ou `Falhou`. Sem isso, "enviei" significa apenas "Expo aceitou na fila".

**2. In-app pro admin — Server-Sent Events**

- Endpoint `GET /v1/notifications/stream` retorna `IAsyncEnumerable<NotificacaoEvent>` com `Content-Type: text/event-stream` e `[Authorize]`.
- Fan-out entre instâncias via Postgres `LISTEN/NOTIFY`: ao gravar notificação `InApp`, executa `pg_notify('notif:empresa:{empresaId}', notificacaoId::text)`. Cada instância .NET mantém 1 `LISTEN` ativo por canal e refrate pra seus SSE clients filtrando por `EmpresaId`.
- Heartbeat a cada 25s (`: keepalive\n\n`) contra timeout de proxy.
- Front Vue assina via `EventSource`; recebe `notificacaoId`, invalida a query de notificações no TanStack Query (`useNotificacoesQuery`), e dispara snackbar + atualiza badge.
- Nginx/Traefik precisa de `X-Accel-Buffering: no` e timeouts longos no path do stream.

### Canais explicitamente fora do dispatcher

**3. WhatsApp — deep-link `wa.me`**

- **NÃO usar Meta Cloud API.**
- Botão "Falar com motorista" no card/detalhe do agendamento abre `https://wa.me/55{telefone}?text={template-encoded}`.
- Templates de mensagem ficam no front (constants TS) por tipo de evento — string com placeholders `{nome}`, `{data}`, `{unidade}`, `{motivo}`. Garante consistência de redação sem aprovação Meta.
- `Motorista.Telefone` já existe no domínio. Backend sanitiza para apenas dígitos com DDI antes de expor no DTO.
- Botão escondido se motorista não tem telefone cadastrado.

**4. SMS — fora do escopo inicial**

- Sem WhatsApp como canal robusto automatizado, SMS isolado como fallback de push não compensa custo (Zenvia/Twilio) + cadastro PJ + manutenção.
- Reavaliar na Onda 2 se push tiver taxa de entrega <90% sustentada.

### Modelo de dados

```
Notificacao
  Id (uuid, pk)
  EmpresaId (uuid, fk, multi-tenant scope)
  DestinatarioUsuarioId (uuid, fk)
  Tipo (enum: AgendamentoCancelado, AgendamentoConfirmado, MotoristaAtrasoInformado, ...)
  Titulo (varchar 120)
  Corpo (text, markdown leve)
  Payload (jsonb — IDs de domínio, ex: { agendamentoId, motivo })
  Prioridade (enum: Normal, Alta, Critica)
  CriadaEm (timestamptz)
  LidaEm (timestamptz, nullable)
  CriadaPor (uuid, nullable — null para eventos automáticos)

NotificacaoEntrega
  Id (uuid, pk)
  NotificacaoId (uuid, fk)
  Canal (enum: Push, InApp)  -- WhatsApp e SMS não entram aqui
  Status (enum: Pendente, Enviado, Falhou, Cancelado)
  TentativasEfetuadas (int)
  ProximaTentativaEm (timestamptz, nullable)
  UltimaTentativaEm (timestamptz, nullable)
  ProviderMessageId (varchar, nullable — ticket Expo)
  Erro (text, nullable)

DispositivoUsuario
  Id (uuid, pk)
  UsuarioId (uuid, fk)
  ExpoPushToken (varchar, unique)
  Plataforma (enum: ios, android)
  AppVersion (varchar)
  UltimoUsoEm (timestamptz)
  Ativo (bool)

UsuarioPreferenciaNotificacao
  UsuarioId (uuid, fk)
  Canal (enum: Push, InApp)
  Tipo (enum)
  Ativo (bool)
  -- pk: (UsuarioId, Canal, Tipo)
```

Índices: `(EmpresaId, DestinatarioUsuarioId, CriadaEm desc)` em `Notificacao`; `(Status, ProximaTentativaEm)` em `NotificacaoEntrega` para claim do worker.

### Disparo via eventos de domínio

Não chamar `INotificacaoService.Criar()` direto dentro dos services de domínio (ex: `CancelarAgendamentoHandler`). Padrão:

1. Service de domínio publica evento (`agendamento.AddDomainEvent(new AgendamentoCanceladoEvent(...))`).
2. `IDomainEventDispatcher` próprio percorre `dbContext.ChangeTracker` no `SaveChangesAsync` e dispara handlers.
3. `NotificacaoEventHandler` decide quem-recebe-o-quê e cria `Notificacao` + `NotificacaoEntrega(Pendente)`.

**Por quê**: as regras "admin recebe quando motorista cancela, motorista recebe quando admin cancela, expiração automática não notifica admin" tendem a virar complexas. Separar evento de handler permite testar regra sem mexer no fluxo crítico de cancelamento.

### Dispatcher

`IHostedService` separado (`NotificacaoDispatcherService`):

```
Loop a cada 5s:
  BEGIN;
  SELECT ... FROM NotificacaoEntrega
    WHERE Status = 'Pendente'
      AND ProximaTentativaEm <= NOW()
    FOR UPDATE SKIP LOCKED
    LIMIT 50;
  -- processa em paralelo controlado (MaxDegreeOfParallelism = 10)
  -- atualiza Status / TentativasEfetuadas / ProximaTentativaEm
  COMMIT;
```

`FOR UPDATE SKIP LOCKED` dá distributed lock natural — funciona com N instâncias, sem Redis.

**Retry por canal**:
- **Push**: 3 tentativas, backoff exponencial 30s / 2min / 10min. Após falha final, fica `Falhou` (sem fallback automático — ver "Alternativas" abaixo).
- **InApp**: marca `Enviado` na hora da criação (a tabela é o canal). Sem retry necessário.

### Decisão pendente com cliente

**Aurora aceita zero auditoria de comunicação WhatsApp?**

Como WhatsApp é deep-link manual disparado pelo admin, não há registro no sistema de que a mensagem foi enviada. Push + in-app cobrem o "comprovante oficial". Se Aurora compliance exigir que toda comunicação com motorista seja logada, o desenho retorna pra **WhatsApp Cloud API + Meta templates** — substituído por novo ADR.

Validar com jurídico/compliance Aurora **antes do início da Sprint 1**.

## Consequências

**Positivas**:
- Implementação ~5× mais rápida que WhatsApp API completo.
- Zero custo recorrente de mensagens (Expo grátis, SSE em PG nativo).
- Zero burocracia Meta (sem verificação, sem templates, sem aprovação).
- WhatsApp deep-link aproveita o número pessoal/corporativo já existente.
- `FOR UPDATE SKIP LOCKED` simplifica infra (sem Redis).
- SSE single-direction é mais leve que WebSocket bidirecional.

**Negativas**:
- Sem rastro de "foi avisado via WhatsApp" no sistema — se motorista alegar "não fui avisado", apenas push + in-app servem como prova.
- Eventos automáticos noturnos (ex: expiração) não disparam WhatsApp — motorista descobre só no próximo login no app.
- Número pessoal do admin fica exposto ao motorista quando clica no `wa.me` (mitigação no admin: usar `WhatsApp Business App` em chip corporativo por fábrica).
- Push depende de Expo (vendor lock parcial — migração pra FCM/APNs direto é possível mas custosa).
- SSE em multi-instance exige `LISTEN/NOTIFY` ativo por instância (pressão pequena mas constante no PG).

## Alternativas consideradas

**A1. WhatsApp Cloud API (Meta) como canal automatizado primário.**
Rejeitada agora, mantida como plano B se compliance exigir. Custo de tempo: +2-3 semanas calendário pra aprovação Meta. Custo recorrente: R$700-1.200/mês em regime. Ganho: auditoria + automação noturna. Tradeoff não compensa pra MVP Aurora.

**A2. Polling do front em vez de SSE.**
Rejeitada. Polling de 20s funciona pra 1 fábrica, mas 80 admins × poll/20s = 4 req/s. Latência percebida ruim (admin cancela e outro admin demora pra ver). SSE custa o mesmo de implementar e entrega real-time real.

**A3. WebSocket via SignalR.**
Rejeitada. Bidirecional é overkill — admin não envia nada nesse canal, só recebe. SSE atravessa proxy/balanceador sem config exótica e reconecta sozinho no `EventSource`.

**A4. Redis pub/sub para fan-out entre instâncias.**
Rejeitada. `LISTEN/NOTIFY` do Postgres resolve sem adicionar nova peça de infra. Voltaria a ser considerado se Redis for adicionado por outro motivo (cache, rate limit distribuído).

**A5. Fallback automático Push → SMS.**
Rejeitada por ora. Sem WhatsApp API como ponte robusta, SMS como fallback isolado não atende custo/benefício. Reavaliar com 60 dias de métrica de entrega de push em produção.

**A6. Não persistir notificação (só pub/sub volátil).**
Rejeitada. Auditoria fiscal Aurora vai pedir "esse motorista foi avisado quando do cancelamento?" — sem tabela não tem resposta. Tabela é fonte da verdade; canais são fan-out em cima.

## Revisão 2026-05-21 — Outbox transacional separada

Esta revisão **adiciona** uma camada de Outbox transacional entre os domain events e a criação da `Notificacao`. O desenho de canais (Expo Push + SSE + WhatsApp deep-link), o modelo de `Notificacao`/`NotificacaoEntrega`/`DispositivoUsuario`/`UsuarioPreferenciaNotificacao` e as decisões de retry por canal **permanecem válidos**.

### O que muda

Fluxo original (seção "Disparo via eventos de domínio"):

```
SaveChangesAsync
  └─ IDomainEventDispatcher percorre ChangeTracker
        └─ NotificacaoEventHandler cria Notificacao + NotificacaoEntrega(Pendente)
```

Fluxo revisado:

```
SaveChangesAsync (mesma TX que muda o domínio)
  └─ IDomainEventDispatcher percorre ChangeTracker
        └─ Para cada evento, INSERT em OutboxEvent (payload jsonb + idempotency_key)

(commit fecha)

OutboxProcessor (IHostedService, separado)
  ├─ LISTEN outbox_new (baixa latência) + polling 10s (fallback)
  ├─ SELECT ... FROM OutboxEvent WHERE ProcessedAt IS NULL FOR UPDATE SKIP LOCKED
  └─ Para cada evento: resolve NotificacaoEventHandler<T>
        └─ Cria Notificacao + NotificacaoEntrega(Pendente)
        └─ UPDATE OutboxEvent SET ProcessedAt = NOW()
```

### Por quê separar Outbox de `NotificacaoEntrega`

O ADR original tratava `NotificacaoEntrega` como a outbox (cada linha pendente = item a despachar). Funciona, mas tem uma fraqueza: se o handler que **decide destinatários** (regra de negócio: quem recebe, quantos canais, qual prioridade) crashar entre o commit do agendamento e a criação da `Notificacao`, a notificação nunca é criada. Não há retry.

Com a Outbox separada:

1. O domínio só publica fato (`AgendamentoCanceladoEvent`) na mesma TX → durabilidade garantida.
2. A regra de fan-out (quem recebe) roda no `OutboxProcessor`, com retry/backoff e idempotência. Se a regra mudar/quebrar, reprocessar é seguro.
3. A `NotificacaoEntrega` continua sendo a outbox **de canais** (push, in-app). Ou seja: Outbox de eventos de domínio é separada da Outbox de canais.

### Tabela `OutboxEvent`

```
OutboxEvent
  Id (uuid, pk)
  EventType (varchar 200 — FQN do evento, ex: "AgendamentoCanceladoEvent")
  Payload (jsonb — serialização do evento de domínio)
  IdempotencyKey (varchar 200, unique — derivado do evento, ex: "agendamento-cancelado:{agendamentoId}")
  EmpresaId (uuid — propagado pra handler reaplicar tenant scope; NÃO usa global filter)
  OcorridoEm (timestamptz — quando o evento aconteceu no domínio)
  CriadoEm (timestamptz — quando entrou na outbox)
  ProcessedAt (timestamptz, nullable — null = pendente)
  Tentativas (int, default 0)
  ProximaTentativaEm (timestamptz, nullable — backoff)
  UltimoErro (text, nullable)
  CorrelationId (uuid — atravessa logs do comando até o dispatch final)
```

**Índices**:
- `(ProcessedAt, ProximaTentativaEm)` parcial onde `ProcessedAt IS NULL` — usado pelo claim do worker.
- `IdempotencyKey` unique — protege contra dupla publicação do mesmo evento.

### Decisões adicionais

**1. `OutboxEvent` NÃO implementa `IEmpresaScoped`.**
O worker roda fora de contexto de request HTTP — não tem `IEmpresaContext` resolvido. Aplicar global filter quebraria o claim. `EmpresaId` é propriedade comum; o handler usa esse valor pra criar `Notificacao` no tenant correto. `Notificacao` e `NotificacaoEntrega` continuam implementando `IEmpresaScoped`.

**2. `LISTEN/NOTIFY` híbrido com polling fallback.**
`pg_notify('outbox_new', event_id)` no commit (interceptor após `SaveChanges`) + polling de segurança a cada 10s pegando órfãos. NOTIFY perde mensagens em restart; polling cobre.

**3. `IdempotencyKey` derivada do evento.**
Padrão sugerido: `"{tipo-evento}:{id-agregado}:{discriminante}"`. Ex: `"agendamento-cancelado:8f...3a"`. Permite dois retries do mesmo comando sem criar dois OutboxEvents.

**4. Pipeline de evolução para RabbitMQ.**
O design atual é "Outbox + Postgres handler direto". Quando o volume justificar (>100k eventos/dia, múltiplos consumers independentes, ou integração ERP externa), só o `OutboxProcessor` muda: passa a publicar pra Rabbit em vez de chamar handler in-process. Domínio, controllers, schema e demais camadas permanecem.

### O que isso muda no que JÁ existe

A classe `TruckFlow.Domain.Entities.Notificacao` hoje é um stub (`Descricao`, `AgendamentoId`, `EmpresaId`). Será substituída pelo schema completo desta seção do ADR (`Tipo`, `Titulo`, `Corpo`, `Payload jsonb`, `Prioridade`, `DestinatarioUsuarioId`, `LidaEm`). A FK direta `Notificacao → Agendamento` é **removida** — o vínculo com agregados de domínio passa pelo `Payload jsonb`, que é genérico por tipo de evento.

### Decisão auth-SSE no browser (2026-05-21)

`EventSource` nativo do browser não suporta header `Authorization`. Avaliadas três opções:

1. **Cookie httpOnly dedicado pro access token** — quebra o padrão atual (access em memória, refresh em cookie). Introduz CSRF surface adicional. Rejeitado.
2. **Token via query string** — vaza em logs de proxy/CDN. Rejeitado em qualquer ambiente.
3. **`@microsoft/fetch-event-source` no front** — lib oficial Microsoft, ~3KB gzip, aceita Bearer header. Mantém access em memória. **Aceito.**

Backend não precisa de mudança: `[Authorize]` continua funcionando com Bearer no header. CORS já libera origens explícitas do front sem necessidade de `AllowCredentials`.

### Reconexão e perda de eventos (2026-05-21)

`Last-Event-ID` server-side foi avaliado e **descartado**. Implementação exigiria ou coluna sequence em `Notificacao` (migration nova) ou ordenação por timestamp (colisões raras mas possíveis). Solução escolhida:

- Tabela `Notificacao` é fonte da verdade.
- Front mantém query TanStack `useNotificacoes()` com cache.
- Ao reconectar SSE (handler `onopen` do `fetch-event-source`), front executa `queryClient.invalidateQueries(['notificacoes'])` — refetch do feed completo do servidor.
- SSE é canal de **notificação push em tempo real**, não fila durável. Durabilidade é responsabilidade do banco.

Trade-off aceito: depois de uma desconexão longa, o front faz 1 query a mais. Pra Aurora (80 admins × poucos reconnects/dia), custo desprezível.

### Pendências pós-pipeline (2026-05-21)

Pipeline backend end-to-end funcionando (Domain Event → Outbox → Worker → Handler → Notificacao → pg_notify → SSE → Browser). Ficam abertos:

**UI Admin (front Vue)** — composables/queries/hooks prontos (`useNotificacoesQuery`, `useNotificacoesUnreadCountQuery`, `useNotificacao().markAsRead`, `useRealtimeNotifications`). Falta o componente visual:

- `NotificationBell.vue` na `Navbar.vue` — `v-badge` com `unreadCount` + `v-menu` listando últimas 10. Cada item clicável: marca como lida + navega pra contexto (ex: `payload.agendamentoId` → `/agendamentos/{id}`).
- Página `/notifications` (opcional) com lista paginada completa + filtros (lidas/não lidas, tipo, período).
- Tradução de `TipoNotificacao` → ícone Material por tipo (cancelado → warning-circle, criado → check, atraso → clock).

**UI Mobile (Expo / React Native)** — integração inteira pendente:

- `expo-notifications` + `expo-device` instalados; pedido de permissão no primeiro launch.
- Backend `DispositivoUsuario` (já modelado no ADR original, mas tabela ainda não criada) + endpoint `POST /v1/dispositivos/registrar` que aceita `expoPushToken`, `plataforma`, `appVersion` (idempotente por token).
- Mobile registra token no login + cold start; backend desativa registro ao receber `DeviceNotRegistered` no receipt da Expo.
- Worker secundário (ou extensão do `OutboxProcessorWorker`) que pega `NotificacaoEntrega(Canal=Push, Status=Pendente)` e chama Expo Push API. Backoff 30s/2min/10min.
- Receipt poller: 2-15min depois do envio, consulta receipt na Expo e atualiza `NotificacaoEntrega.Status` para `Enviado` ou `Falhou`.
- Notification handler no app: tap → deep-link pra tela relevante baseado em `payload.agendamentoId`.

**Suite de testes** — ver [ADR-0004 item 14](./0004-prerequisitos-rollout-aurora.md). Gate multi-tenant é **não-negociável** antes do rollout Aurora.

**Eventos de domínio adicionais** — só `AgendamentoCanceladoEvent` está publicando hoje. Pra cobrir o escopo do ADR (admin → motorista e motorista → admin), implementar conforme prioridade Aurora:
- `AgendamentoConfirmadoEvent` (admin confirma reserva)
- `AgendamentoReagendadoEvent` (mudança de janela)
- `AgendamentoExpiradoEvent` (não-show)
- `MotoristaAtrasoInformadoEvent` (motorista declara atraso via app)
- `MotoristaChegouEvent` / `MotoristaSaiuEvent` (check-in/out)
- `JanelaProximaEvent` (worker que dispara aviso 30min antes do `DataFim`)

Cada um precisa de evento + handler. Reusa toda a infra existente (Outbox, Worker, Listener, SSE) — só adiciona `IDomainEventHandler<T>` correspondente.

**LISTEN/NOTIFY no OutboxProcessor** (otimização) — hoje o worker usa polling 2s. Adicionar `LISTEN outbox_new` + `pg_notify('outbox_new', id)` no `OutboxSaveChangesInterceptor` reduz latência percebida pra <100ms sem mudar arquitetura. Polling fica como fallback de segurança contra mensagens perdidas em restart. Não bloqueia rollout.

### Ordem de implementação (núcleo primeiro)

1. **Núcleo**: `OutboxEvent`, `Notificacao` (revista), `NotificacaoEntrega` + migration única.
2. `IDomainEventDispatcher` + interceptor de `SaveChangesAsync` (eventos do `ChangeTracker` viram `OutboxEvent` na mesma TX).
3. `OutboxProcessor` (`IHostedService`) com `FOR UPDATE SKIP LOCKED` + LISTEN/NOTIFY.
4. `NotificacaoEventHandler` por tipo de evento (fan-out).
5. SSE endpoint + connection manager.
6. `DispositivoUsuario` + integração Expo Push.
7. `UsuarioPreferenciaNotificacao` (opt-in/opt-out granular).
8. Observabilidade + métricas.

## Referências

- [ADR-0001 — Alvo Aurora](./0001-alvo-aurora.md)
- [ADR-0004 — Pré-requisitos de infra](./0004-prerequisitos-rollout-aurora.md) (multi-instance + distributed lock são pré-condição)
- Mobile: integração push é detalhada no ADR-0001 do repo mobile.
- Admin: consumo de SSE é detalhado no ADR-0001 do repo admin.
