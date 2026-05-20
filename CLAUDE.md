# TruckFlow — Backend (API)

Backend do sistema TruckFlowApp. Cliente-alvo: **Aurora Alimentos** (piloto 1 fábrica → 21 simultâneas).

## Stack

- **.NET 8.0** (ASP.NET Core Web API, C#)
- **Entity Framework Core 9.0** + **Npgsql** (PostgreSQL)
- **AspNetCore.Identity** (usuários, roles, hash de senha)
- **JWT Bearer** (`System.IdentityModel.Tokens.Jwt`) — HMAC-SHA256, chave 512-bit
- **Serilog 4.3** (logging)
- **xUnit 2.5** (testes)

## Arquitetura (Clean Architecture multi-projeto)

```
src/TruckFlowApi/
├── TruckFlow/                  # API entrypoint (Program.cs, Controllers, Extensions, Middlewares, Filters)
├── TruckFlow.Application/      # Services (AuthService, UsuarioService, ...)
├── TruckFlow.Domain/           # Entities, DTOs, Interfaces (contratos)
├── TruckFlowApi.Infra/         # AppDbContext, Migrations, Repositórios
├── TruckFlow.Services/         # Integrações externas (SEFAZ, ...)
└── TruckFlow.Test/             # xUnit (atualmente sem testes de auth)
```

**Extensions/** em `TruckFlow/Extensions/` agrupa DI por feature (Auth, Cors, Swagger, etc) — adicionar nova feature = nova subpasta de extension.

## Banco de dados

- **PostgreSQL** via Npgsql.
- Migrations EF Core em `src/TruckFlowApi/TruckFlowApi.Infra/Migrations/`.
- Rodar: `cd src/TruckFlowApi/TruckFlow && dotnet ef database update`
- Criar nova: `dotnet ef migrations add <Nome> --project ../TruckFlowApi.Infra --startup-project .`

## Autenticação atual

- Login admin: `POST /v1/AuthAdmin/login` → `AuthAdminController.cs:32`
- Login motorista: `POST /v1/AuthMotorista/login` → `AuthMotoristaController.cs`
- Geração JWT em `TruckFlow.Application/AuthService.cs:34-86`
- Expiração: **4h fixas** (sem refresh token — em implementação)
- Claims: `Name`, `Email`, `UserId`, `EmpresaId` (multi-tenant), `MotoristaId` se for motorista, Roles do Identity
- Setup do middleware: `TruckFlow/Extensions/Auth/AuthInjection.cs` — `ClockSkew=0`, valida Issuer/Audience/Key

## Multi-tenant

- `Usuario.EmpresaId` (Guid?) é a chave de tenant.
- Vai no JWT como claim e deve ser usado em **todo** filter de query (`.Where(x => x.EmpresaId == empresaId)`).
- Já implementado em UsuarioService — seguir o padrão.

## CORS

`TruckFlow/Extensions/Cors/AddCorsDependencyInjection.cs` — libera:
- `http://localhost:5173` (front Vue dev)
- `https://truck-flow-app.vercel.app` (front prod)
- `http://192.168.18.6:8080` (rede interna / mobile dev)

Quando subir cookies httpOnly: vai precisar `.AllowCredentials()` e origins explícitas (não `*`).

## Config / segredos

- Dev: `dotnet user-secrets` (não commitar `appsettings.Development.json` com chaves).
- Prod: env vars com `__` (ex.: `JwtOptions__SecurityKey`, `ConnectionStrings__DefaultConnection`).
- Chaves esperadas: `JwtOptions:SecurityKey`, `JwtOptions:Issuer=TruckFlow`, `JwtOptions:Audience=TruckFlow`.

## ADRs (decisões arquiteturais)

Em `Docs/adr/`:
- `0001-alvo-aurora.md` — dimensionamento Aurora
- `0002-design-notificacoes.md` — push Expo + SSE + WhatsApp deep-link
- `0003-design-tracking-motorista.md` — foreground + geofencing + TimescaleDB + LGPD 90d
- `0004-prerequisitos-rollout-aurora.md` — 16 itens não-feature

**Sempre consultar ADRs antes de mudar design de auth/notificações/tracking.**

## Convenções

- Nada de mock em testes de integração — usar Postgres real (feedback Aurora: prod migration quebrou com mock).
- DTOs em `Domain/Dtos/`, sufixo `Dto`.
- Services retornam DTOs, nunca entidades EF.
- Controllers magros: validação + chamada de service + retorno.
- Async/await em tudo que toca BD.
