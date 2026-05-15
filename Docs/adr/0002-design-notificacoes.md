# ADR-0002 — Sistema de notificações: push + SSE in-app; WhatsApp via deep-link

- **Status**: Aceito
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

## Referências

- [ADR-0001 — Alvo Aurora](./0001-alvo-aurora.md)
- [ADR-0004 — Pré-requisitos de infra](./0004-prerequisitos-rollout-aurora.md) (multi-instance + distributed lock são pré-condição)
- Mobile: integração push é detalhada no ADR-0001 do repo mobile.
- Admin: consumo de SSE é detalhado no ADR-0001 do repo admin.
