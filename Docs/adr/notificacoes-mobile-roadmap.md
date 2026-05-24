# Notificações no mobile — roadmap pendente

- **Status**: Pendente
- **Data**: 2026-05-21
- **Contexto**: Pipeline backend completo (Outbox → Worker → Handler → Notificacao → pg_notify → SSE) está funcionando. Admin recebe via SSE no front Vue. Falta o canal Push pro motorista.
- **ADRs relacionados**: [ADR-0002](./0002-design-notificacoes.md) (decisão de Expo Push), [ADR-0001](./0001-alvo-aurora.md) (dimensionamento).

## O que precisa ser feito

### 1. Schema backend — `DispositivoUsuario`

Tabela já definida no ADR-0002 mas **ainda não criada** (Aurora usa só SSE no admin por enquanto):

```
DispositivoUsuario
  Id (uuid, pk)
  UsuarioId (uuid, fk)
  ExpoPushToken (varchar, unique)
  Plataforma (enum: ios, android)
  AppVersion (varchar)
  UltimoUsoEm (timestamptz)
  Ativo (bool)
```

Migration nova: `add-dispositivo-usuario`.

### 2. Endpoint de registro de token

```
POST /v1/dispositivos/registrar
Authorization: Bearer <motorista-jwt>
Body: { expoPushToken, plataforma, appVersion }
```

Idempotente por `expoPushToken`. Se token já existe pra outro usuário, transfere (motorista trocou de conta no mesmo aparelho). Se token vier marcado `DeviceNotRegistered` pela Expo num envio futuro, marca `Ativo = false`.

### 3. Mobile — `expo-notifications` + `expo-device`

```bash
expo install expo-notifications expo-device
```

No app:
- Pedir permissão de notificações no primeiro launch.
- Após login do motorista (`/v1/AuthMotorista/login`), pegar `expoPushToken` via `Notifications.getExpoPushTokenAsync()` e enviar pro backend.
- Re-enviar em cold start (token pode rotacionar).
- Handler de notification tap: extrair `payload.agendamentoId` e fazer deep-link pra tela de agendamento.

### 4. Push Dispatcher (worker secundário)

Hoje a `NotificacaoEntrega(Canal=Push, Status=Pendente)` é criada pelo handler mas **ninguém despacha**. Adicionar:

- `PushDispatcherWorker : BackgroundService` — varre `NotificacaoEntrega WHERE Canal=Push AND Status=Pendente AND ProximaTentativaEm <= NOW()` via `FOR UPDATE SKIP LOCKED LIMIT 50`.
- Pra cada entrega: busca todos os `DispositivoUsuario` ativos do destinatário, monta payload Expo, chama `POST https://exp.host/--/api/v2/push/send` em batches (Expo aceita até 100 messages por request).
- Resposta da Expo retorna `ticket` por mensagem; salva em `NotificacaoEntrega.ProviderMessageId`.
- Backoff: 30s → 2min → 10min → falha definitiva (mesmo padrão do OutboxProcessor).

### 5. Receipt Poller

Expo Push API só aceita o envio — a entrega real pra FCM/APNs pode falhar (token inválido, app desinstalado). Pra confirmar entrega:

- `PushReceiptPollerWorker : BackgroundService` — 2-15 minutos após envio, chama `POST https://exp.host/--/api/v2/push/getReceipts` com lista de ticket IDs.
- Atualiza `NotificacaoEntrega.Status` pra `Enviado` (receipt ok) ou `Falhou` (com `Erro` da Expo).
- Se receipt vier com `DeviceNotRegistered`, marca `DispositivoUsuario.Ativo = false` (não tenta mais).

### 6. UI Mobile — Tela de Notificações

Mesma endpoint `GET /v1/notifications` que o admin usa, autenticando com JWT do motorista. Backend já filtra por `DestinatarioUsuarioId == _user.UserId`.

Componentes mobile:
- Lista paginada com pull-to-refresh.
- Badge no ícone do app (via `Notifications.setBadgeCountAsync(count)`).
- `markAsRead` ao abrir o detalhe.

### 7. Mobile — "Avisar empresa" (motorista → admins)

Endpoint backend já existe: `POST /v1/notifications/send-empresa` `[Authorize(Roles=Motorista)]`. Body:

```json
{ "agendamentoId": "uuid", "titulo": "string", "corpo": "string" }
```

Backend resolve a empresa via agendamentoId, valida que motorista é o dono do agendamento, busca todos os admins (Role=Admin com EmpresaId = empresa do agendamento) e cria 1 Notificacao por admin. Real-time funciona automaticamente — admins online recebem via SSE.

Componentes mobile a implementar:
- Botão "Avisar fábrica" no detalhe do agendamento (visível enquanto status ∈ {Agendado, EmAndamento}).
- Modal com `titulo` (TextInput) + `corpo` (TextArea, multiline).
- POST pro endpoint com Bearer JWT do motorista.
- Tratamento de erro 401/403 (token expirado, agendamento alheio).

Casos de uso típicos pra Aurora:
- "Vou atrasar 30 min" → admin reagenda.
- "Cheguei mas não consegui contato" → admin checa porteiro.
- "Carga divergente da nota" → admin manda equipe de conferência.

## Ordem de implementação sugerida

1. Migration `DispositivoUsuario` + endpoint de registro (~0.5d).
2. Mobile: pedido de permissão + envio do token (~0.5d).
3. `PushDispatcherWorker` (~1d).
4. `PushReceiptPollerWorker` (~0.5d).
5. UI mobile básica (~1d).
6. Teste de gate multi-tenant push (ver [ADR-0004 item 14](./0004-prerequisitos-rollout-aurora.md)).

**Total**: ~3.5d de trabalho. Pré-requisito de Aurora (motorista precisa receber push, não dá pra contar com motorista deixar app aberto).
