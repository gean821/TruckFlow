# TruckFlow — Backend (API)

Backend do sistema TruckFlowApp. Cliente-alvo: **Aurora Alimentos** (piloto 1 fábrica → 21 simultâneas).

## Repositórios do produto

| Repo | Caminho | Papel |
|---|---|---|
| API (este) | `C:\ESTUDO\TruckFlow` | .NET 8 + PostgreSQL |
| Admin web | `C:\ESTUDO\TruckFlowApp` | Vue 3 + Vuetify (Admin, Gerente, Monitor, Portaria) |
| App motorista | `C:\tf-mobile` | Expo / React Native (Motorista) |

## Stack

- **.NET 8.0** (ASP.NET Core Web API, C#)
- **Entity Framework Core 9.0** + **Npgsql 9** (PostgreSQL 16)
- **AspNetCore.Identity** (usuários, roles, hash de senha)
- **JWT Bearer** (`System.IdentityModel.Tokens.Jwt`) — HMAC-SHA256, chave 512-bit + **refresh token rotation**
- **Microsoft.Identity.Web** — SSO Entra ID (OIDC) opcional por tenant
- **FluentValidation 12** (chamado manualmente nos services)
- **Zeus.Net.NFe.NFCe** (consulta SEFAZ / NF-e)
- **Serilog 4.3** (logging)
- **xUnit 2.5 + Testcontainers.PostgreSql** (testes com Postgres real); NBomber (load) e BenchmarkDotNet

## Arquitetura (Clean Architecture multi-projeto)

```
src/TruckFlowApi/
├── TruckFlow/                  # API entrypoint (Program.cs, Controllers, Extensions, Middlewares, Filters)
├── TruckFlow.Application/      # Services, Interfaces/, Exceptions/, Factories/, Validators/, Sefaz/, Notificacoes/
├── TruckFlow.Domain/           # Entities, Enums, Dto/<Feature>/, Contracts, Events, Rules
├── TruckFlowApi.Infra/         # AppDbContext, EntitiesMapping, Interceptors, Migrations, Repositórios, Outbox
├── TruckFlow.Services/         # stub vazio, fora da .sln — não usar (SEFAZ vive em Application/Sefaz)
├── TruckFlow.Test/             # xUnit: Domain/, Application/, Integration/ (concorrência, cross-tenant, dispatcher)
├── TruckFlow.Benchmarks/       # BenchmarkDotNet
└── TruckFlow.LoadTests/        # NBomber
```

**Extensions/** em `TruckFlow/Extensions/` agrupa DI por feature (Auth, Cors, Swagger, etc) — adicionar nova feature = nova subpasta de extension, com `AddXxx()` chamado no `Program.cs`.

## Banco de dados

- **PostgreSQL** via Npgsql (`docker compose up -d` sobe `postgres:16-alpine` local na 5432).
- Migrations EF Core em `src/TruckFlowApi/TruckFlowApi.Infra/Migrations/`.
- Rodar: `cd src/TruckFlowApi/TruckFlow && dotnet ef database update`
- Criar nova: `dotnet ef migrations add <Nome> --project ../TruckFlowApi.Infra --startup-project .`
- Hoje a API também aplica migrations no startup (`db.Database.Migrate()` no `Program.cs`) — sair disso é o item 2 do ADR-0004.
- Concorrência: `xmin` como concurrency token (Agendamento, ItemPlanejamento, Notificacao, NotificacaoEntrega, OutboxEvent, DispositivoUsuario) + exclusion constraint `agendamento_no_overlap_per_doca` (btree_gist).

## Autenticação atual

- Login admin: `POST /v1/AuthAdmin/login` → `AuthAdminController.cs:39`
- Login motorista: `POST /v1/AuthMotorista/login` → `AuthMotoristaController.cs`
- SSO Entra ID (staff): `AuthEntraController.cs` — habilitado por empresa em `Empresa.Configuracoes.authMethods`; detalhes em `Docs/entra-id-integracao-backlog.md`
- Geração JWT em `TruckFlow.Application/AuthService.cs`; refresh em `RefreshTokenService.cs`
- Access token: **15 min** (`JwtOptions:AccessLifetimeMinutes`). Refresh token: **30 dias** com rotação por família (`RefreshTokenOptions:LifetimeDays`), armazenado como hash
- Web: refresh em cookie httpOnly `tf_refresh` (`RefreshCookieHelper`) → `POST /v1/Auth/refresh`, `POST /v1/Auth/logout`
- Mobile: refresh no body → `POST /v1/Auth/refresh-mobile`, `POST /v1/Auth/logout-mobile`; `POST /v1/Auth/logout-all` revoga todas as sessões
- Claims: `Name`, `Email`, `UserId`, `EmpresaId` (multi-tenant), `MotoristaId` se for motorista, Roles do Identity
- Papéis: `Admin`, `Gerente`, `Monitor`, `Portaria`, `Motorista` — grupos em `Domain/Entities/Roles.cs` (`RoleGroups`)
- Setup do middleware: `TruckFlow/Extensions/Auth/AuthInjection.cs` — `ClockSkew=0`, valida Issuer/Audience/Key

## Multi-tenant

- Tenant = `Empresa`. Entidades de tenant implementam **`IEmpresaScoped`** → filtro global `HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)` no `AppDbContext` + `EmpresaScopedSaveChangesInterceptor` validando escrita.
- `EmpresaId` vem da claim do JWT (`CurrentUserGuard.GetEmpresaId()`), **nunca do body**. Sem tenant no contexto, o filtro lança `UnauthorizedAccessException`.
- Cross-tenant só em métodos `...AcrossTenants` (`IgnoreQueryFilters()`) + `IEmpresaContext.WithTenant(...)`; workers rodam cada empresa dentro de `WithTenant`.
- `Usuario` **não** é `IEmpresaScoped` — leituras/escritas de usuário filtram `EmpresaId` manualmente.
- Variação entre fábricas vai em `Empresa.Configuracoes` (jsonb), não em código condicional.

## Erros

`ExceptionHandlingMiddleware` devolve envelope `{ success: false, code, message }`:
`NotFoundException` → 404 · `BusinessException`/`DomainException` → 400 · `BadRequestException` → 400 `BAD_REQUEST` · `ValidationException` (FluentValidation) → 400 `VALIDATION_ERROR` com `errors[]` · `UnauthorizedAccessException` → 401 · resto → 500.

## CORS

`TruckFlow/Extensions/Cors/AddCorsDependencyInjection.cs` — origens de `Cors:AllowedOrigins` (config), com fallback para:
- `http://localhost:5173` (front Vue dev)
- `https://truck-flow-app.vercel.app` (front prod)
- `http://192.168.18.6:8080` (rede interna / mobile dev)

Já usa `.AllowCredentials()` (necessário para o cookie de refresh) — manter origins explícitas (nunca `*`).

## Config / segredos

- Dev: `dotnet user-secrets` (não commitar `appsettings.Development.json` com chaves).
- Prod: env vars com `__` (ex.: `JwtOptions__SecurityKey`, `ConnectionStrings__DefaultConnection`).
- Chaves esperadas: `JwtOptions:SecurityKey`, `JwtOptions:Issuer=TruckFlow`, `JwtOptions:Audience=TruckFlow`, `EntraIdOptions:*`, `Sefaz:*` (`UseFake=true` por padrão).

## Deploy

Railway, auto-deploy no push da `master` (Dockerfile `src/TruckFlowApi/TruckFlow/Dockerfile`, porta 8080, healthcheck `/health/ready`). Único ambiente: produção.

## ADRs (decisões arquiteturais)

Em `Docs/adr/`:
- `0001-alvo-aurora.md` — dimensionamento Aurora
- `0002-design-notificacoes.md` — push Expo + SSE + WhatsApp deep-link
- `0003-design-tracking-motorista.md` — foreground + geofencing + LGPD 90d (infra revisada em `Docs/rastreamento-motorista-backlog.md`: Postgres puro, sem TimescaleDB)
- `0004-prerequisitos-rollout-aurora.md` — 16 itens não-feature

**Sempre consultar ADRs antes de mudar design de auth/notificações/tracking.**

## Convenções

- Nada de mock em testes de integração — usar Postgres real (feedback Aurora: prod migration quebrou com mock). Fixture: `TruckFlow.Test/Integration/Fixtures/DatabaseFixture.cs`.
- DTOs em `Domain/Dto/<Feature>/`: `XxxCreateDto`, `XxxUpdateDto`, `XxxListQueryDto`, `XxxResponseDto`; listagens retornam `PagedResponse<T>`.
- Services retornam DTOs, nunca entidades EF — mapeamento manual com `private static MapToResponse(...)`.
- Controllers magros: chamada de service + retorno (validação FluentValidation fica no service).
- Async/await em tudo que toca BD; `CancellationToken` como último parâmetro.
- Regras completas: `.claude/RULES.md`. Agentes e skills do projeto em `.claude/agents/` e `.claude/skills/`.

## Manter este arquivo vivo

Quando algo descrito aqui for implementado, mudado ou deixar de ser verdade (item "em implementação" concluído, caminho que mudou, decisão nova), **atualize este CLAUDE.md na mesma entrega**. Vale também para os CLAUDE.md do admin e do mobile.

## Git

- Nunca incluir linhas de atribuição ao Claude (`Co-Authored-By: Claude...`, `Claude-Session: ...` etc.) em mensagens de commit ou PR. Mensagens de commit só com o conteúdo do próprio commit.
