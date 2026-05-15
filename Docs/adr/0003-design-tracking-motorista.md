# ADR-0003 — Rastreamento de motorista: foreground-only + TimescaleDB

- **Status**: Aceito (escopo: Onda 1)
- **Data**: 2026-05-14

## Contexto

Aurora pediu visibilidade em tempo real da localização dos motoristas que confirmaram agendamento, para:

1. Antecipar chegada e ajustar fila de descarga.
2. Detectar atraso significativo e cancelar/reagendar com proatividade.
3. Auditoria de trajeto em caso de incidente (carga, NF, multa).

Dados existentes no domínio:
- `UnidadeEntrega.Latitude` / `Longitude` (destino).
- `Empresa.Latitude` / `Longitude` (fábrica).
- `Motorista` não tem campo de localização persistida.

Restrições:
- **LGPD**: localização é dado pessoal sensível. Exige consentimento informado, retenção definida, direito de exclusão.
- **Bateria do motorista**: ping muito frequente derruba aparelho em viagem de longa distância.
- **App stores**: Android `ACCESS_BACKGROUND_LOCATION` é revisado caso-a-caso pela Play Store; iOS exige justificativa forte na App Store. Ambos podem rejeitar/atrasar release em semanas.
- **Volume em regime ([ADR-0001](./0001-alvo-aurora.md))**: ~400 motoristas × 2880 pings/dia × 365 dias = 421M rows/ano se mantiver histórico completo.

## Decisão

### Escopo Onda 1: foreground-only com geofencing

Captura de localização **apenas com app em foreground** (ou via foreground service Android com notificação persistente — não exige `ACCESS_BACKGROUND_LOCATION`). Background tracking adiado para Onda 2, **se** for justificável e aprovado nas stores.

**Geofencing automático**:
- App para de enviar pings quando:
  - motorista entra em raio de 200m da `Empresa.Latitude/Longitude`, **OU**
  - agendamento transita para `EmAndamento` ou `Finalizado` ou `Cancelado`.
- Backend também ignora pings nesses estados (defesa em profundidade).
- Reduz consumo de bateria + atende princípio LGPD de minimização (motorista dentro da fábrica não precisa ser rastreado).

### Modelo de dados

**Tabela hot (última posição, 1 linha por motorista)**:

```
MotoristaPosicaoAtual
  MotoristaId (uuid, pk)
  AgendamentoId (uuid, fk)
  EmpresaId (uuid, fk, multi-tenant scope)
  Latitude (double precision)
  Longitude (double precision)
  Accuracy (real, em metros)
  Velocidade (real, m/s, nullable)
  Heading (real, graus, nullable)
  CapturadoEm (timestamptz)  -- timestamp do GPS no aparelho
  RecebidoEm (timestamptz)   -- timestamp do servidor
```

Índices: `(EmpresaId, AgendamentoId)`, `(EmpresaId, CapturadoEm desc)`.

**Hypertable de histórico**:

```
MotoristaPosicaoHistorico  -- TimescaleDB hypertable, partition by CapturadoEm dia
  MotoristaId (uuid)
  AgendamentoId (uuid)
  EmpresaId (uuid)
  Latitude (double precision)
  Longitude (double precision)
  Accuracy (real)
  Velocidade (real, nullable)
  CapturadoEm (timestamptz)
```

**Policies TimescaleDB**:
- `add_compression_policy('MotoristaPosicaoHistorico', INTERVAL '7 days')` — compressão automática de chunks >7 dias (~90% compression ratio em geo).
- `add_retention_policy('MotoristaPosicaoHistorico', INTERVAL '90 days')` — drop automático.

90 dias é default — **valor final a confirmar com jurídico Aurora antes da Sprint 4**.

### Endpoints

**Ingestão (motorista)**:

```
POST /v1/motorista/posicao
  [Authorize(Roles = "Motorista")]
  
  body: {
    agendamentoId: uuid,
    posicoes: [
      { lat, lng, accuracy, velocidade?, heading?, capturadoEm }
    ]
  }
```

Regras:
- **Sempre batch** (`posicoes: [...]`) mesmo que mande 1 ponto. Mobile acumula offline em `expo-sqlite` e flusha quando reconecta.
- `MotoristaId` vem do `ICurrentUserService.MotoristaId` (claim do JWT). **Nunca do body** — mesmo bug listado pro `/reservar` em [ADR-0004](./0004-prerequisitos-rollout-aurora.md).
- Validar `agendamentoId` pertence ao motorista E está em status `Agendado`/`EmAndamento`. Se não, retornar 403 sem detalhe.
- Validar timestamps razoáveis (`capturadoEm` não no futuro, não >1h no passado).
- **Rate limit**: 1 req/5s por motorista (com batch isso é suficiente).
- **Upsert atômico**: transação curta, raw SQL `INSERT ... ON CONFLICT (MotoristaId) DO UPDATE` no `MotoristaPosicaoAtual` + `COPY` ou batch insert no histórico. **Não usar EF tracking** — é hot path.
- Idempotency-Key header opcional (cliente repetindo requisição).

**Consulta (admin)**:

```
GET /v1/agendamentos/{id}/localizacao
  [Authorize(Roles = "Admin,Operador")]
  -- última posição + ETA estimado

GET /v1/agendamentos/{id}/localizacao/historico?desde=...
  -- trajeto (timestamp inicial obrigatório)

GET /v1/empresas/{empresaId}/motoristas/localizacoes
  -- mapa único da fábrica: todos motoristas ativos a caminho
  -- response cacheado em servidor por 10s (todos os admins vendo o mesmo dado)
```

**Direito de exclusão LGPD**:

```
DELETE /v1/motorista/eu/historico-localizacao
  [Authorize(Roles = "Motorista")]
  -- purge imediato do histórico + log no audit trail
  -- audit log mantém o evento de exclusão sem o dado
```

### Real-time pro front admin

Mesmo padrão SSE de [ADR-0002](./0002-design-notificacoes.md):

- Ao receber batch, backend executa `pg_notify('posicao:empresa:{empresaId}', JSON com motoristaId+lat+lng+timestamp)`.
- Throttle no servidor: emite no SSE no máximo **1×/5s por motorista**, mesmo se chegarem 5 pings nesse intervalo. Browser não precisa de mais que isso e poupa fan-out.
- Endpoint dedicado: `GET /v1/empresas/{empresaId}/motoristas/localizacoes/stream`.

### LGPD — não opcional

Pré-requisitos antes de captura em produção:

1. **Aviso explícito no app antes da 1ª captura** ("Sua localização será compartilhada com a fábrica X enquanto agendamento ativo. Pode revogar nas configurações do app.").
2. **Tela de preferências no app** com toggle "Compartilhar localização durante agendamentos" (default: false até motorista aceitar termos).
3. **Endpoint DELETE** funcional e testado.
4. **Encarregado de dados Aurora documentado** no contrato.
5. **Retenção 90 dias** ou outro valor alinhado com jurídico Aurora.
6. Página de termos atualizada no app + admin com a descrição da finalidade.

### Google Maps no admin

API key separada, com restrição **HTTP referrer** (admin) e **bundle id** (mobile, se vier a usar SDK).

- Cofre: **Azure Key Vault / AWS Secrets / GCP Secret Manager / SOPS**. Não em `.env` versionado, não em `appsettings.json`.
- Maps JavaScript API: ~R$80-100/mês em regime Aurora (free tier US$200 cobre quase tudo).
- **Distance Matrix server-side é proibido** para cálculo de ETA contínuo — custo proibitivo em regime (US$2.880/mês). ETA é calculado cliente-side via Maps JS (reaproveita free tier) ou OSRM self-hosted.

## Consequências

**Positivas**:
- Foreground-only evita gargalo das app stores no MVP.
- Geofencing economiza bateria do motorista E reduz dados armazenados.
- TimescaleDB resolve vacuum/storage com 2 linhas de policy, sem fazer particionamento manual.
- LGPD endereçada por design, não bolted-on depois.
- Batch endpoint resiliente a perda de conectividade na rodovia.

**Negativas**:
- ETA preditivo (ex: "motorista a 30min — atrase o slot") fica limitado sem background tracking.
- Foreground service Android exige notificação persistente — pode incomodar motorista.
- TimescaleDB exige extension instalada no Postgres (provider managed precisa suportar: Aiven sim, RDS apenas em algumas versões, Supabase sim).
- Custo de Maps Distance Matrix cliente-side é compartilhado entre admins — picos de uso podem extrapolar free tier.

## Alternativas consideradas

**A1. Background location desde a Onda 1.**
Rejeitada. Risco de rejeição/atraso pela Play Store e App Store é alto. Motorista chegando à unidade tipicamente fica com app aberto na tela do ticket — foreground cobre o caso real.

**A2. Postgres puro (sem TimescaleDB) com particionamento manual.**
Rejeitada. 421M rows/ano sem partitioning vira pesadelo de vacuum. Particionamento manual via `pg_partman` ou DDL custom é viável mas exige manutenção e tooling adicional. TimescaleDB é 1 extension `CREATE EXTENSION` + 2 policies.

**A3. Redis com TTL pra última posição + S3 pra histórico.**
Rejeitada. Adiciona 2 peças de infra (Redis cluster + S3 bucket + lifecycle policy) pra resolver o que TimescaleDB resolve nativo. Ainda complica audit/replay porque Redis é volátil.

**A4. Receber `motoristaId` do body em vez do JWT, mais "flexível".**
Rejeitada agressivamente. É exatamente o bug que está no `/reservar` hoje (motorista pode reservar em nome de outro). Mesmo padrão na ingestão de localização seria pior — motorista mal-intencionado pluga GPS falso e desabona colega.

**A5. Não usar geofencing, deixar app sempre enviando.**
Rejeitada. LGPD pede minimização. Motorista dentro da fábrica não precisa ser rastreado — princípio de finalidade e necessidade. Geofencing entrega isso sem custo perceptível de UX.

**A6. Google Maps SDK no mobile (em vez de só deep-link).**
Rejeitada para MVP. SDK adiciona ~12MB ao bundle + key + revisão. Deep-link via `expo-linking` resolve o caso de uso "abrir navegação até o destino" com 0 dependência extra. Detalhe no ADR-0003 do repo mobile.

## Referências

- [ADR-0001 — Alvo Aurora](./0001-alvo-aurora.md)
- [ADR-0002 — Sistema de notificações](./0002-design-notificacoes.md) (mesmo padrão SSE + `LISTEN/NOTIFY`)
- [ADR-0004 — Pré-requisitos de infra](./0004-prerequisitos-rollout-aurora.md) (extension TimescaleDB, secrets vault)
- Mobile: captura de localização detalhada no ADR-0002 do repo mobile.
- Admin: tela de mapa detalhada no ADR-0002 do repo admin.
