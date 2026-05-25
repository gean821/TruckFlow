# Notificações realtime via SSE — plano de implementação

> Documento vivo. Cada bloco abaixo é debatido e validado individualmente; quando aprovado, a decisão final é gravada aqui com a data e os trade-offs descartados.
>
> **Referência arquitetural macro**: [ADR-0002 — Sistema de notificações](../adr/0002-design-notificacoes.md).
> Este documento aprofunda **execução**: schema concreto, contratos de código, sequência de operações, observabilidade.

## Sumário

- [Visão geral (diagrama)](#visão-geral-diagrama)
- [Estado atual do código](#estado-atual-do-código)
- [Mapa de blocos de decisão](#mapa-de-blocos-de-decisão)
- [Decisões validadas](#decisões-validadas)

---

## Diagrama explicativo (para apresentação a leigos)

Cola direto no Excalidraw via "Insert → Mermaid to Excalidraw".

```mermaid
flowchart TB
    subgraph Pessoas["👥 Quem usa"]
        Admin["👨‍💼 Admin<br/>(navegador na fábrica)"]
        Motorista["🚚 Motorista<br/>(celular no caminhão)"]
    end

    subgraph Sistema["🏢 Sistema TruckFlow"]
        API["⚙️ Servidor<br/>(recebe e responde)"]
        DB[("💾 Banco de dados<br/>(memória do sistema)")]
        Fila["📬 Fila de eventos<br/>(coisas a processar)"]
        Trabalhador["👷 Trabalhador<br/>(processa a fila)"]
        Megafone["📡 Megafone interno<br/>(avisa todo mundo)"]
    end

    subgraph Externos["☁️ Serviços externos"]
        Google["🔔 Google / Apple<br/>(notificação na tela bloqueada)"]
    end

    Admin -->|"1️⃣ digita mensagem<br/>ou cancela agendamento"| API
    API -->|"2️⃣ guarda na memória"| DB
    API -->|"2️⃣ se for evento importante,<br/>joga na fila"| Fila

    Trabalhador -->|"3️⃣ pega da fila<br/>e prepara notificação"| Fila
    Trabalhador -->|"4️⃣ salva notificação"| DB

    DB -->|"5️⃣ assim que salva,<br/>avisa o megafone"| Megafone

    Megafone -->|"6️⃣ chat aberto?<br/>chega na hora"| Motorista
    Megafone -->|"6️⃣ chat aberto?<br/>chega na hora"| Admin

    Trabalhador -->|"7️⃣ celular fechado?<br/>envia push"| Google
    Google -->|"8️⃣ toca o sino<br/>na tela bloqueada"| Motorista

    classDef pessoas fill:#dbeafe,stroke:#1e40af,color:#1e3a8a
    classDef sistema fill:#f3f4f6,stroke:#374151,color:#111827
    classDef externos fill:#fef3c7,stroke:#92400e,color:#78350f
    class Admin,Motorista pessoas
    class API,DB,Fila,Trabalhador,Megafone sistema
    class Google externos
```

### Os 3 caminhos que uma notificação percorre

```mermaid
flowchart LR
    subgraph A["🅰️ Mensagem de chat<br/>(rápido)"]
        direction TB
        A1[Admin digita] --> A2[Servidor salva]
        A2 --> A3[Megafone interno]
        A3 --> A4[📱 Motorista vê na hora]
    end

    subgraph B["🅱️ Evento automático<br/>(cancelamento, chegada...)"]
        direction TB
        B1[Algo acontece<br/>no agendamento] --> B2[Evento na fila]
        B2 --> B3[Trabalhador processa]
        B3 --> B4[Cria notificação<br/>no banco]
        B4 --> B5[📱 Aparece na hora<br/>se app aberto]
    end

    subgraph C["©️ App fechado<br/>(precisa do sininho)"]
        direction TB
        C1[Notificação salva] --> C2[Servidor pede ajuda<br/>ao Google]
        C2 --> C3[Google manda sinal<br/>pro celular]
        C3 --> C4[🔔 Sininho toca<br/>mesmo bloqueado]
    end

    classDef ok fill:#dcfce7,stroke:#166534,color:#14532d
    class A4,B5,C4 ok
```

### Por que precisa de tudo isso

```mermaid
flowchart TB
    Q{"Onde está o motorista<br/>agora?"}

    Q -->|"App aberto, chat aberto"| R1["✨ Vê instantâneo<br/>via megafone interno (SSE)"]
    Q -->|"App aberto, outra tela"| R2["✨ Vê instantâneo<br/>+ toast no topo"]
    Q -->|"App em segundo plano"| R3["🔔 Recebe push<br/>na barra de notificações"]
    Q -->|"Celular bloqueado/desligado"| R4["🔔 Push acumula<br/>aparece quando ligar"]

    R1 --> Result[("✅ Em todos os cenários,<br/>a mensagem chega")]
    R2 --> Result
    R3 --> Result
    R4 --> Result

    classDef resp fill:#dbeafe,stroke:#1e40af
    classDef ok fill:#dcfce7,stroke:#166534
    class R1,R2,R3,R4 resp
    class Result ok
```

---

## Visão geral (diagrama)

Diagrama em Mermaid (importa direto no Excalidraw via "Insert → Mermaid to Excalidraw"):

```mermaid
flowchart TB
    subgraph App["Application Layer"]
        AS[AgendamentoService.Cancelar]
        AS -->|raises| DE[AgendamentoCanceladoEvent]
    end

    subgraph Persist["Persistence (mesma transação)"]
        DE --> UPD[UPDATE Agendamento]
        DE --> INS[INSERT OutboxEvent]
        UPD --> COMMIT[(COMMIT)]
        INS --> COMMIT
        COMMIT -->|trigger ou interceptor| NOTIFY[pg_notify outbox_new]
    end

    subgraph Worker["OutboxProcessor (IHostedService, por réplica)"]
        NOTIFY --> LISTEN[LISTEN outbox_new]
        POLL[Polling fallback 10s] -.->|órfãos| WORK
        LISTEN --> WORK[Resolve handler]
        WORK --> CRIAR[Cria Notificacao + NotificacaoEntrega]
        CRIAR --> DISP[Dispatch nos canais]
    end

    subgraph Canais["Dispatchers"]
        DISP --> SSE_LOCAL[SSE local in-memory]
        DISP --> PUSH[Expo Push HTTP]
        DISP --> FANOUT[pg_notify notif_fanout]
    end

    subgraph Réplicas["Outras réplicas API"]
        FANOUT --> SSE_REMOTO[SSE em réplica B]
    end

    SSE_LOCAL --> NAV[Admin browser]
    SSE_REMOTO --> NAV
    PUSH --> APP[Motorista mobile]
```

Diagrama de sequência do fluxo "admin cancela → motorista vê":

```mermaid
sequenceDiagram
    autonumber
    participant Admin as Admin (Browser)
    participant API as API .NET
    participant DB as Postgres
    participant Worker as OutboxProcessor
    participant Push as Expo Push
    participant App as Motorista (App)

    Admin->>API: POST /agendamentos/{id}/cancelar
    API->>DB: BEGIN
    API->>DB: UPDATE Agendamento
    API->>DB: INSERT OutboxEvent (AgendamentoCancelado)
    API->>DB: COMMIT
    DB-->>Worker: pg_notify outbox_new
    Worker->>DB: SELECT FOR UPDATE SKIP LOCKED
    Worker->>DB: INSERT Notificacao + NotificacaoEntrega
    Worker->>Push: POST /send (Expo Push API)
    Push-->>App: notificação chega
    App->>API: GET /notifications/agendamento/{id} (ao abrir)
    API-->>App: lista atualizada
```

---

## Estado atual do código

Mapeado em 2026-05-24 (revisado após auditoria do backend completo):

### Backend — pipeline SSE pronto ✅

| Componente | Arquivo | Status |
|---|---|---|
| `OutboxEvent` (entidade) | `TruckFlow.Domain/Entities/OutboxEvent.cs` | ✅ |
| `Notificacao`, `NotificacaoEntrega`, `DispositivoUsuario` (entidades) | `TruckFlow.Domain/Entities/` | ✅ |
| `OutboxEventSerializer` | `TruckFlowApi.Infra/Outbox/` | ✅ |
| **Domain events pattern** (`IDomainEvent`, `IDomainEventHandler<T>`) | `TruckFlow.Domain.Events` | ✅ |
| **OutboxProcessorWorker** com `FOR UPDATE SKIP LOCKED`, backoff exponencial 30s→10min, max 8 tentativas | `TruckFlow/Extensions/Notificacao/OutboxProcessorWorker.cs` | ✅ |
| **Endpoint SSE** `GET /v1/notifications/stream` | `TruckFlow/Controllers/NotificationsStreamController.cs` | ✅ |
| **SseNotificationStreamer** — keepalive 25s, headers `X-Accel-Buffering: no`, formato `id:/event:/data:` | `TruckFlow/Extensions/Notificacao/SseNotificationStreamer.cs` | ✅ |
| **INotificationConnectionManager** — `Channel<T>` por usuário | `Application/Notificacoes/` | ✅ |
| **RealtimeNotificationInterceptor** — coleta `Notificacao` Added em `SaveChanges`, dispara `pg_notify('notif_realtime', payload)` no `SavedChanges` (after commit) | `TruckFlowApi.Infra/Database/Interceptors/RealtimeNotificationInterceptor.cs` | ✅ |
| **RealtimeNotificationListener** — `BackgroundService` mantém `LISTEN notif_realtime` com reconnect 5s | `TruckFlow/Extensions/Notificacao/RealtimeNotificationListener.cs` | ✅ |
| `PushDispatcherWorker` + `ReceiptPollerWorker` | `TruckFlow/Extensions/Notificacao/` | ✅ |
| Handler `AgendamentoCanceladoEvent` | `Application/Notificacoes/Handlers/AgendamentoCanceladoNotificacaoHandler.cs` | ✅ |

### Web — conectado no SSE ✅

| Componente | Arquivo | Status |
|---|---|---|
| Composable `useRealtimeNotifications` usando `@microsoft/fetch-event-source` (resolve header `Authorization` em SSE) | `truckflow.app/src/composables/useRealtimeNotifications.ts` | ✅ |
| Reconnect automático com backoff; FatalError em 401/403 pra parar reconnect em loop | mesmo arquivo | ✅ |
| `invalidateQueries` ao receber evento + toast por prioridade | mesmo arquivo | ✅ |

### Mobile — único gap real ❌

| Componente | Status |
|---|---|
| Lib SSE client | ❌ `react-native-sse` não instalado |
| Hook `useRealtimeNotifications` mobile | ❌ Não existe |
| Integração no `AvisarFabricaModal` | ⚠ Hoje usa polling 5s |

### Refinamentos pendentes (não bloqueantes)

| Componente | Status |
|---|---|
| Handlers de eventos além de `AgendamentoCancelado` (Confirmado, Reagendado, MotoristaChegou, MensagemManual) | ❌ |
| Resume com `Last-Event-ID` (header no controller + cursor no streamer + índice) | ❌ |
| `UsuarioPreferenciaNotificacao` (silenciar tipos de evento por usuário) | ❌ — adiar pra pós-MVP |
| Observabilidade: correlation ID atravessando comando → evento → outbox → dispatch | ⚠ Logs existem, falta correlation ID |

---

## Mapa de blocos — status após auditoria

Os blocos 1-7 estão **implementados**. Restam blocos focados em fechar o ciclo mobile + refinamentos.

| # | Bloco | Status |
|---|---|---|
| 1 | Modelo de dados (`OutboxEvent`, `Notificacao`, `NotificacaoEntrega`, `DispositivoUsuario`) | ✅ |
| 2 | Domain events (`IDomainEvent`, `IDomainEventHandler<T>`) | ✅ |
| 3 | Outbox processor (`OutboxProcessorWorker` com SKIP LOCKED + backoff) | ✅ |
| 4 | Worker lifecycle (`BackgroundService` por réplica) | ✅ |
| 5 | NotificationDispatcher (handlers por tipo de evento) | ✅ |
| 6 | SSE endpoint + connection manager (`Channel<T>` por user) | ✅ |
| 7 | Multi-instance fan-out (`pg_notify('notif_realtime', payload)` + `RealtimeNotificationListener`) | ✅ |
| 8 | Auth SSE (Bearer via `fetchEventSource` no web; Bearer via `react-native-sse` no mobile) | ✅ web / ❌ mobile |
| 9 | Reconexão + `Last-Event-ID` resume | ❌ (refinamento) |
| 10 | Frontend hooks (Vue ✅ via `@microsoft/fetch-event-source`; RN ❌) | ⚠ |
| 11 | Observabilidade (correlation ID atravessando o pipeline) | ⚠ falta correlation ID |
| 12 | Teste integração multi-tenant como gate de merge | ❌ |

### Backlog real de execução

1. **[P0]** Instalar `react-native-sse` no mobile + hook `useRealtimeNotifications` espelhando o do web.
2. **[P0]** Plugar hook no `AvisarFabricaModal`: ao receber evento, invalida queryKey; pode baixar polling pra 30s como safety net ou remover.
3. **[P1]** Adicionar handlers para outros eventos: `AgendamentoConfirmado`, `AgendamentoReagendado`, `MotoristaChegou`, `MensagemManualMotorista`, `MensagemManualAdmin`.
4. **[P2]** Resume `Last-Event-ID`: controller lê header → streamer faz query inicial filtrando `Notificacao.Id > cursor` antes de entrar no pump.
5. **[P2]** Correlation ID propagado via `Activity.Current` ou cabeçalho `X-Correlation-Id` atravessando comando → outbox → handler → SSE.
6. **[P3]** Teste de integração com 2 empresas validando isolamento.

---

## Decisões validadas

### 2026-05-24 — Mobile plugado no SSE + 2 novos handlers de evento

**Mobile:**
- Lib: `react-native-sse` (pure JS, sem rebuild nativo).
- Hook: `src/hooks/useRealtimeNotifications.ts` — autenticação via header `Authorization: Bearer {token}` (a lib RN aceita headers, diferente do `EventSource` nativo do web).
- Reativo ao `token` do Zustand (logout fecha conexão; login reabre).
- Listener `notification` → invalida `notificacaoQueryKey`, `notificacaoUnreadCountQueryKey`, `notificacaoAgendamentoQueryKey` + toast por prioridade.
- Chamado uma vez no `app/_layout.tsx` dentro do `InitialLayout`.
- `pollingInterval: 5000` na lib (heartbeat de reconnect interno; não é polling de query).
- `AvisarFabricaModal`: polling do TanStack baixado de 5s pra 30s (apenas safety net se SSE cair silenciosamente).

**Backend — 2 eventos de domínio novos:**
- `AgendamentoEvent.MotoristaChegouEvent` — raised em `Agendamento.RegistrarChegada()`. Handler `MotoristaChegouNotificacaoHandler` notifica **todos admins da empresa** (canal InApp apenas).
- `AgendamentoEvent.AgendamentoReagendadoEvent` — raised em `Agendamento.Reagendar(novaInicio, novaFim)` (novo método de domínio com guard de "datas iguais → no-op" + restrição de "só se motorista já reservou"). Handler `AgendamentoReagendadoNotificacaoHandler` notifica o **motorista** (canais InApp + Push).
- Service `AgendamentoAdminService.Update` agora chama `agendamento.Reagendar(...)` em vez de setar `DataInicio`/`DataFim` direto.
- Ambos os eventos agrupados em `AgendamentoEvent.cs` como `static partial class` com records aninhados (padrão consistente pra eventos relacionados a uma entidade).
- Handlers registrados em `NotificacaoDependencyInjection`.

**Mensagens manuais (`MensagemManualMotorista`/`MensagemManualAdmin`):**
- **Não precisam de outbox/domain event/handler.** O `NotificacaoSendService` grava `Notificacao` direto, e o `RealtimeNotificationInterceptor` pega no `SaveChanges` → `pg_notify` → SSE.
- Caminho mais curto + mais barato.

### Fluxo end-to-end agora ativo

```
[Admin browser]  ─POST /agendamentos/X/cancelar→  [API]
                                                    ├─ Agendamento.Cancelar() raise event
                                                    ├─ OutboxSaveChangesInterceptor → INSERT OutboxEvent
                                                    └─ COMMIT

[OutboxProcessor]  ─FOR UPDATE SKIP LOCKED→  [DB]
                    ├─ resolve handler (AgendamentoCanceladoNotificacaoHandler)
                    ├─ cria Notificacao + NotificacaoEntrega (InApp + Push)
                    └─ SaveChanges
                        ├─ RealtimeNotificationInterceptor → pg_notify('notif_realtime', payload)
                        └─ PushDispatcherWorker → Expo Push (canal Push pendente)

[pg_notify]  ─broadcast→  [todas as réplicas API]
                            └─ RealtimeNotificationListener.OnNotification
                                └─ ConnectionManager.PublishToUser(motoristaUserId, evt)
                                    └─ SSE Channel<T> → mobile/web conectado

[Mobile/Web]  ─useRealtimeNotifications→  invalidateQueries
                                          + toast
```
