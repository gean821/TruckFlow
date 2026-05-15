# ADR-0004 — Pré-requisitos de infra para rollout Aurora

- **Status**: Aceito
- **Data**: 2026-05-14

## Contexto

Avaliação inicial do projeto classificou vários itens de infra/segurança como **Onda 3** (evolução pós-go-live). Reanálise considerando alvo de [ADR-0001](./0001-alvo-aurora.md) (21 fábricas Aurora) reclassifica vários como **pré-condição de rollout**, não evolução.

A razão é simples: volume puro não é o gargalo (qualquer single-instance roda 2k caminhões/dia). O que mata é confiabilidade num cliente fiscal sério onde:

- 1 episódio de downtime em deploy = 21 fábricas paradas simultâneas;
- 1 vazamento entre tenants = problema competitivo entre marcas concorrentes da própria cooperativa;
- Sem RPO/RTO documentado, jurídico Aurora não assina contrato;
- Sem pen-test e CI, due-diligence técnico não passa.

## Decisão

Lista canônica de itens **não-feature** que precisam estar prontos antes do rollout pleno (e parcialmente antes do piloto fiscal). Estimativa total: ~16 dias úteis.

### Gates por fase

| # | Item | Esforço | Gate |
|---|---|---|---|
| 1 | 2+ instâncias da API atrás de reverse-proxy (nginx ou Traefik) | 2-3d | Pré-piloto |
| 2 | Migrations em job separado, fora do `Program.cs` | 0.5d | Pré-piloto |
| 3 | HEALTHCHECK no Dockerfile + endpoint `/health` (DB, worker, Expo reach) | 0.5d | Pré-piloto |
| 4 | Distributed lock no `AgendamentoExpirationService` via `FOR UPDATE SKIP LOCKED` | 0.5d | Pré-piloto |
| 5 | Secrets em vault (Key Vault / SOPS) — remove `appsettings.json` e `docker-compose.yml` versionados | 0.5d | Pré-piloto |
| 6 | Fix do filtro multi-tenant `EmpresaId == Guid.Empty` retornando `true` | 0.5d | Pré-piloto |
| 7 | Backup PostgreSQL: `pg_dump` diário + WAL archiving contínuo | 1d | Pré-piloto |
| 8 | PgBouncer ou pool equivalente | 0.5d | Pré-rollout |
| 9 | OpenTelemetry → Prometheus/Grafana | 2d | Pré-rollout |
| 10 | Logs centralizados (Seq self-hosted ou Grafana Loki) | 1d | Pré-rollout |
| 11 | Refresh token + httpOnly cookie (remove JWT em localStorage do admin) | 2d | Pré-piloto |
| 12 | Idempotency-Key header em `/reservar` e `POST /motorista/posicao` | 1d | Pré-piloto |
| 13 | CI no GitHub Actions: build + test + scan | 1d | Pré-rollout |
| 14 | Suite de testes mínima: `Reservar` concorrente, transições de status, filtro multi-tenant, dispatcher | 3d | Pré-piloto |
| 15 | Load test simulando 21 fábricas (k6 ou Bombardier) | 1d | Pré-rollout |
| 16 | Penetration test por terceiro | 2-5d cliente | Pré-rollout |

### Detalhamento dos itens não-óbvios

**Item 1 — Multi-instance + reverse-proxy**: nginx ou Traefik como front. Pelo menos 2 instâncias da API rotacionando deploys (blue/green ou rolling). Sticky session **não é necessária** porque API é stateless + SSE reconecta sozinho. Custo operacional adicional: ~R$200/mês.

**Item 2 — Migrations fora do startup**: hoje `Program.cs` faz `db.Database.Migrate()` na inicialização. Em multi-instance, 2 instâncias subindo competem por `__EFMigrationsHistory`. Mover migrations para **job separado** no pipeline de deploy (step "db-migrate" antes de "deploy-api").

**Item 4 — Distributed lock**: o `AgendamentoExpirationService` hoje roda como `IHostedService`. Com 2 instâncias, ambas tentam expirar o mesmo agendamento. Solução: claim com `FOR UPDATE SKIP LOCKED` no `SELECT` da query de expiração — mesmo padrão do `NotificacaoDispatcherService` em [ADR-0002](./0002-design-notificacoes.md).

**Item 5 — Secrets em vault**: JWT signing key está em `appsettings.json:12` versionado. Senha do DB em `appsettings.json:3` e `docker-compose.yml:9`. Mover para `dotnet user-secrets` em dev e variáveis de ambiente (provisionadas via Key Vault / SOPS) em prod. **A rotação da JWT signing key em produção está pendente** — alerta já registrado em memória do agente.

**Item 6 — Multi-tenant filter bug**: `AppDbContext.cs:94` (referência conforme estado do código em maio/2026) tem fallback do filtro global que retorna `true` quando `EmpresaId == Guid.Empty` (ou seja, sem contexto de usuário setado). Em 21 tenants, qualquer endpoint que esqueça de popular `ICurrentUserService` vaza dados de outras empresas. **Trocar fallback para retornar zero linhas** + cobrir com teste de integração.

**Item 7 — Backup**: `pg_dump` diário num volume separado (S3, Backblaze, Azure Blob) + WAL archiving contínuo (`archive_command` no `postgresql.conf`) habilita PITR (Point-in-Time Recovery). RPO alvo: 5 minutos. RTO alvo: 30 minutos. Considerar **Postgres managed (Aiven, RDS, Supabase)** com replica desde a Sprint 1 — Aurora não vai querer cuidar de Postgres na unha.

**Item 9 — Observabilidade**: OpenTelemetry no .NET (`OpenTelemetry.Extensions.Hosting` + `Instrumentation.AspNetCore` + `Instrumentation.EntityFrameworkCore` + `Exporter.Prometheus.AspNetCore`). Métricas críticas: latência de `Reservar`, taxa de cancelamento, lag do dispatcher, % de push delivered, conexões PG em uso. Alertas via webhook (Slack/Discord) para SLO violations. **Grafana Cloud free tier** cobre o volume.

**Item 11 — Refresh token + httpOnly**: JWT em `localStorage` é vulnerável a XSS. Refresh token em cookie `HttpOnly + Secure + SameSite=Strict`, access token em memória (Pinia/store volátil). Mobile fica como está (SecureStore já é seguro).

**Item 12 — Idempotency-Key**: cliente repetindo requisição (timeout, retry de rede, double-click protegido no front mas não no back) não pode duplicar reserva ou registrar 2 vezes a mesma posição. Header `Idempotency-Key: {uuid}` armazenado por 24h numa tabela `IdempotencyKey` ou no Redis. Stripe pattern.

**Item 14 — Testes**: `TruckFlow.Test/` hoje tem só `.csproj`. Cobertura mínima inegociável antes do piloto:
- `Reservar` com 100 threads concorrentes batendo no mesmo slot (deve resultar em 1 sucesso e 99 falhas de concorrência, não em 2+ reservas).
- Transições de status do agendamento (todas as combinações Allowed/Denied).
- Filtro multi-tenant (consulta sem contexto de usuário deve retornar zero linhas).
- Dispatcher de notificação processa pendentes, faz retry com backoff, marca falha após N tentativas.

## Consequências

**Positivas**:
- Risco operacional do rollout cai drasticamente.
- Aurora due-diligence técnico passa sem retrabalho.
- Time desenvolve hábitos corretos cedo (testes, secrets vault, observabilidade).
- Custo de troubleshooting em produção cai (logs centralizados + métricas).

**Negativas**:
- ~16 dias úteis não-feature antes do piloto fiscal. Pressão de prazo pode tentar pular itens.
- Custo de hosting aumenta vs. single-instance (~R$400-800/mês adicional para 2 instâncias + Postgres managed + observabilidade).
- Time precisa absorver práticas operacionais que não tinham antes (OTel, k6, pen-test).

## Alternativas consideradas

**A1. Adiar itens 8-10, 13, 15-16 para pós-piloto.**
Parcialmente aceita: itens marcados "Pré-rollout" podem ficar para depois do piloto fiscal numa fábrica, mas não para depois do início do rollout pra 21. Diferença prática: piloto pode rodar com menos infra, mas rollout escalado não.

**A2. Skip itens 9-10 e usar logs locais + checagem manual.**
Rejeitada para rollout pleno. 21 fábricas operando = impossível debugar incidentes via SSH em container individual. Logs centralizados são pré-condição.

**A3. Skip pen-test e fazer apenas SAST automatizado no CI.**
Rejeitada. Aurora padrão de due-diligence inclui pen-test por terceiro independente. Não é opcional contratualmente.

## Referências

- [ADR-0001 — Alvo Aurora](./0001-alvo-aurora.md)
- [ADR-0002 — Sistema de notificações](./0002-design-notificacoes.md)
- [ADR-0003 — Rastreamento de motorista](./0003-design-tracking-motorista.md)
- Memória persistente do agente: `alert_jwt_rotation.md` — referência ao JWT signing key não rotacionado.
