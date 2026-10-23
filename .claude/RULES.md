# Regras do Projeto (TruckFlow)

Este documento define padrões práticos dos **três repositórios** do TruckFlow para orientar código novo e manter consistência com o estilo e a arquitetura atuais. Ele complementa (não substitui) os `CLAUDE.md` de cada repo e os ADRs em `Docs/adr/`.

| Repo | Caminho | Papel |
|---|---|---|
| API | `C:\ESTUDO\TruckFlow` | .NET 8 + EF Core 9 + PostgreSQL 16 |
| Admin web | `C:\ESTUDO\TruckFlowApp` | Vue 3 + Vuetify 3 + TanStack Vue Query (pnpm) |
| App motorista | `C:\tf-mobile` | Expo 54 + RN 0.81 + TanStack Query + Zustand (npm) |

Cliente-alvo: **Aurora Alimentos** — piloto 1 fábrica → 21 fábricas simultâneas, marcas internas **concorrentes entre si** (ADR-0001). Vazamento entre `Empresa`s é incidente competitivo, não bug cosmético.

> Hierarquia quando houver conflito: **ADR aceito > código atual > CLAUDE.md > este arquivo > docs legados**. Se um doc diverge do código, **corrija o doc na mesma entrega** (CLAUDE.md do repo afetado, este arquivo) — não espalhe avisos de "defasado".

---

## Backend (.NET)

### Stack e fundamentos

- **.NET 8**, ASP.NET Core Web API, C# com `Nullable` e `ImplicitUsings` habilitados.
- **EF Core 9.0.9 + Npgsql 9.0.4** (PostgreSQL 16). Identity (`IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>`).
- **FluentValidation 12** — chamado **manualmente** no service (não há auto-validation).
- **JWT Bearer** (HMAC-SHA256, `ClockSkew = 0`) + refresh token rotation (cookie httpOnly no web, body no mobile) + **Entra ID OIDC** por tenant (`Empresa.Configuracoes.authMethods`).
- **Serilog 4.3** (Console + File JSON) via `ILogger<T>`.
- **Zeus.Net.NFe.NFCe** para SEFAZ (síncrono, embrulhado em `Task.Run`).
- **xUnit + Testcontainers.PostgreSql** (Postgres real), `Assert` puro, fakes à mão. NBomber (load) e BenchmarkDotNet.
- Deploy: **Railway**, auto-deploy no push da `master` (Dockerfile em `src/TruckFlowApi/TruckFlow/Dockerfile`, healthcheck `/health/ready`).

### Estrutura (Clean Architecture multi-projeto)

```
src/TruckFlowApi/
├── TruckFlow/                 # API: Controllers/, Extensions/<Feature>/, Middlewares/, Filters/, Configuration/
├── TruckFlow.Application/     # Services (raiz), Interfaces/I<X>Service, Exceptions/, Factories/, Validators/<Feature>/, Sefaz/, Notificacoes/
├── TruckFlow.Domain/          # Entities/, Enums/, Dto/<Feature>/, Dto/Shared/, Contracts/, Events/, Exceptions/, Rules/
├── TruckFlowApi.Infra/        # Database/AppDbContext, Database/EntitiesMapping/*Configuracao, Database/Interceptors/, Repositories/, Outbox/, Migrations/
└── TruckFlow.Test/            # Domain/, Application/, Integration/ (Fixtures, Fakes)
```

- `TruckFlow.Services` é stub vazio fora da `.sln` — **não adicione código lá**.
- Dependências reais: API → Application, Domain, Infra; Application → Domain, **Infra** (interfaces de repositório vivem em `Infra/Repositories/Interfaces`). Não "corrija" a direção das dependências sem um ADR — é refactor grande.

### Fluxo de uma request (padrão a seguir)

```
Controller (magro) → Service (validação + regra + tenant) → Factory (Dto → Entity) / métodos da Entity
  → Repositório (EF) → SaveChanges (interceptors: tenant, audit, outbox, realtime)
  → MapToResponse (Entity → ResponseDto) → Controller retorna Ok/CreatedAtAction/NoContent
Erros: throw BusinessException/NotFoundException/DomainException/ValidationException → ExceptionHandlingMiddleware
```

### Nomenclatura

- Vocabulário do domínio em **português**; código novo com métodos em português ou inglês **consistentes dentro do arquivo** (não misture `GetById` com `ObterPorId` no mesmo service).
- `XxxController`, `IXxxService`/`XxxService`, `IXxxRepositorio`/`XxxRepositorio`, `XxxConfiguracao`, `XxxFactory`, `XxxCreateValidator`/`XxxUpdateValidator`.
- DTOs em `Domain/Dto/<Feature>/`: entrada `XxxCreateDto`, `XxxUpdateDto`; listagem `XxxListQueryDto`; saída — **código novo usa `XxxResponseDto`** (o legado mistura `XxxResponse`; não renomeie o legado sem pedido).
- Nome do arquivo = nome da classe (há legado divergente, ex.: `RecebimentoController.cs` contém `RecebimentosController`).

### Controllers

- `[ApiController]` + `[Route("v1/[Controller]")]` (ou rota explícita kebab/plural já usada: `v1/usuarios`, `v1/notifications`). Sem barra inicial.
- Autorização **só por papel**: `[Authorize(Roles = RoleGroups.X)]` na classe e override por action. **Nunca string literal** (`"Motorista"`) — use `Roles.Motorista`.
- Papéis: `Admin`, `Gerente`, `Monitor`, `Portaria`, `Motorista`. Grupos em `Domain/Entities/Roles.cs` (`CanManageGrade`, `CanCheckIn`, `CanViewSchedule`, `CanManageUsers`, `CanManageMasterData`, `CanViewAuditLogs`).
- Retorno: `Task<IActionResult>`; `CreatedAtAction(nameof(GetById), new { id }, dto)` em POST, `Ok(dto)` em GET/PUT, `NoContent()` em PATCH de ação/DELETE.
- `CancellationToken ct = default` **sempre o último parâmetro** e repassado até o EF.
- `[FromBody]`/`[FromRoute]`/`[FromQuery]` explícitos; rota com `{id:guid}` em código novo.
- Controller **não** lê usuário/tenant, **não** valida, **não** faz `if (x == null) return NotFound()` (o service lança). Validação inline com `return BadRequest("...")` quebra o envelope — não faça.
- Endpoints anônimos (`[AllowAnonymous]` ou sem `[Authorize]`) só para login/refresh/registro público **revisado** — e nunca aceitando `EmpresaId` do body.

### Services, DTOs e mapeamento

- Interfaces em `Application/Interfaces/`. Services **retornam DTOs, nunca entidades EF**.
- Mapeamento **manual**: `private static XxxResponseDto MapToResponse(Xxx e)` no fim do service. Sem AutoMapper. Em leituras paginadas pesadas, projeção `.Select(x => new XxxResponseDto { ... })` no repositório é aceita.
- Validação: `var r = await _createValidator.ValidateAsync(dto, ct); if (!r.IsValid) { _logger.LogWarning(...); throw new ValidationException(r.Errors); }`. Validators registrados por DTO no `Extensions/<Feature>`.
- Validators **não** usam `DateTime.Now` (avaliado na construção e em hora local) — compare em UTC dentro de `Must(...)`.
- Not found: `?? throw new NotFoundException("Grade não encontrada.")`.
- Datas: armazenar **UTC** (`DateTime.UtcNow`), converter para `Grade.OperationalTimeZone` (America/Sao_Paulo) só na borda. `CreatedAt` **não tem default no banco** — set explícito na factory/serviço.
- Transação explícita quando há mais de um `SaveChanges` lógico: `await using var tx = await _repo.BeginTransactionAsync(ct); ... await tx.CommitAsync(ct);`.

### Multi-tenant (não negociável)

- Toda entidade de tenant implementa **`IEmpresaScoped`** → recebe `HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)` automaticamente e passa pelo `EmpresaScopedSaveChangesInterceptor` (preenche/valida `EmpresaId` em escrita).
- Sem tenant no contexto o filtro **lança** `UnauthorizedAccessException("Tenant não identificado.")` (o fallback `Guid.Empty → true` do ADR-0004 item 6 já foi corrigido — não reintroduza fallback permissivo).
- `EmpresaId` vem **da claim** (`CurrentUserGuard.GetEmpresaId()`), **nunca do body**. `MotoristaId` idem (`ICurrentUserService.MotoristaId`).
- Cross-tenant só em métodos de repositório com sufixo **`...AcrossTenants`** usando `IgnoreQueryFilters()`, seguidos de `using var _ = _empresaContext.WithTenant(x.EmpresaId);`.
- `Usuario` **não** é `IEmpresaScoped` → toda leitura/escrita de usuário admin checa `EmpresaId` manualmente (`FindByIdAsync` sozinho vaza).
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` e SQL cru **não passam pelos interceptors** (tenant, audit, outbox) → filtre `EmpresaId` explicitamente e avalie a perda de auditoria.
- Workers iteram empresas e executam cada uma em `empresaContext.WithTenant(empresa.Id)` com try/catch por empresa.
- Variação entre fábricas vai em `Empresa.Configuracoes` (jsonb), **nunca** `if (empresaId == ...)` no código.

### Erros e exceções

Hierarquia (`Application/Exceptions/`, `Domain/Exceptions/`):

| Exceção | HTTP | Envelope |
|---|---|---|
| `NotFoundException` (`HttpResponseException`, code `NOT_FOUND`) | 404 | `{ success:false, code, message }` |
| `BusinessException` (`HttpResponseException`, code `BUSINESS_ERROR`) | 400 | `{ success:false, code, message }` |
| `DomainException` (invariante da entidade) | 400 | `{ success:false, code:"BUSINESS_ERROR", message }` |
| `FluentValidation.ValidationException` | 400 | `{ success:false, code:"VALIDATION_ERROR", errors:[...] }` |
| `UnauthorizedAccessException` | 401 | `{ success:false, message }` |
| qualquer outra | 500 | prod: mensagem genérica; dev: stack |

- `BadRequestException` **não** herda `HttpResponseException` e vira 500 — não use em código novo (use `BusinessException`) até ser corrigida.
- Erros do Postgres viram `BusinessException` com mensagem acionável em PT-BR: `DbUpdateConcurrencyException` (xmin), `SqlState 23505` (unique), `23P01` (exclusion — sobreposição na doca). Padrão em `AgendamentoMotoristaService.BookAppointment`.
- Invariante de estado fica **na entidade** (`Agendamento.AlterarStatus` + `StatusAgendamentoRules.PodeTransitarPara`) lançando `DomainException`.
- Mensagens para o usuário em **português**, dizendo o que fazer ("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.").

### Domínio, eventos e workers

- `EntidadeBase`: `Id` Guid gerado no cliente, `CreatedAt/UpdatedAt/DeletedAt`, `CreatedBy/UpdatedBy` (preenchidos pelo `AuditSaveChangesInterceptor`), `DomainEvents`.
- Efeito colateral (notificação, push) é **evento de domínio** → `OutboxSaveChangesInterceptor` → `OutboxProcessorWorker` (`FOR UPDATE SKIP LOCKED`, backoff, `MaxTentativas = 8`) → `IDomainEventHandler<T>`. Não chame serviço de notificação inline no caso de uso.
- Enums com valor int explícito — **nunca reordene** (persistido e consumido pelo mobile).
- Soft delete é só a coluna `DeletedAt`, **sem filtro global** — quem usa (Usuario, Empresa) filtra à mão.
- Todo `BackgroundService` precisa ser seguro com **2+ instâncias** (ADR-0004): claim com `FOR UPDATE SKIP LOCKED` ou concorrência otimista.

### Dados e repositórios

- Um repositório `sealed` por agregado com primary constructor `(AppDbContext db)`. Sem repositório genérico, sem Unit of Work.
- Assinatura honesta: `Task<T?>` quando pode não achar (legado `return x!;` é bug).
- `AsNoTracking()` em leitura; `Include` só do que o `MapToResponse` usa; listagem de tela **sempre paginada**: `XxxListQueryDto` (`PageNumber`, `PageSize`, filtros, `Search`) → `PagedResponse<T>` (`Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`).
- Concorrência otimista com `xmin` (`builder.Property<uint>("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`).
- Async em tudo que toca BD; nada de `.Result`/`.GetAwaiter().GetResult()` em código novo.

### Banco e migrations

- Tabela = nome da entidade (`ToTable(nameof(Entity))`), identificadores PascalCase entre aspas no SQL (`"Agendamento"."DataInicio"`).
- Criar: `cd src/TruckFlowApi/TruckFlow && dotnet ef migrations add <Nome> --project ../TruckFlowApi.Infra --startup-project .`
- **Sempre revisar o SQL gerado** (`dotnet ef migrations script`). Índice em tabela quente: `CREATE INDEX CONCURRENTLY` via `migrationBuilder.Sql(..., suppressTransaction: true)`. Coluna nova em tabela grande: nullable → backfill → constraint.
- Nunca renomear/remover coluna consumida por um build mobile publicado sem período de compatibilidade.
- Hoje as migrations rodam no startup (`db.Database.Migrate()` no `Program.cs`) — com 2 instâncias vira race (ADR-0004 item 2). Não adicione mais lógica de startup que dependa disso.

### DI (Extensions)

- Uma pasta `TruckFlow/Extensions/<Feature>/` com `static class XxxDependencyInjection { public static IServiceCollection AddXxx(this IServiceCollection services) }`, chamada no `Program.cs`. Confira que o método **é chamado** (bug real: `VeiculoDependencyInjection.AddProduto` nunca é chamado).
- Não registre o mesmo repositório em várias features (use `TryAddScoped`).
- HTTP externo via `IHttpClientFactory` (typed/named client) com timeout; preferir `AddStandardResilienceHandler()` em clientes novos.
- Options pattern (`Configure<XOptions>`) para config; segredo **nunca** em `appsettings*.json` (user-secrets em dev, env vars `Secao__Chave` em prod).

### Logging

- `ILogger<T>` com template estruturado, placeholders PascalCase, mensagem em PT-BR: `_logger.LogInformation("Agendamento {AgendamentoId} reservado (empresa {EmpresaId})", id, empresaId);`
- Sem interpolação de string no template, sem segredos, sem token, sem senha, sem CPF/telefone completos.
- O `RequestLoggingMiddleware` e o `ActionLoggingFilter` já logam request/duração — não duplique no controller.
- Auditoria é automática (`AuditSaveChangesInterceptor`) — não escreva `AuditLog` à mão.

### Testes

- **Nada de mock de banco** em teste de integração — Postgres real via `DatabaseFixture` (Testcontainers `postgres:16-alpine`, migrations reais, `ResetAsync` com TRUNCATE entre testes).
- `[Collection(IntegrationCollection.Name)]` + `IntegrationTestBase`; services montados via `ServiceFactory`; dados via `TestDataSeeder`; usuário via `FakeCurrentUserService`.
- Nome: `Metodo_Cenario_Resultado` em português.
- Toda entidade `IEmpresaScoped` nova ganha **teste cross-tenant** (usuário da empresa B não lê nem altera dado da A).
- Mudança em reserva/status/worker ganha teste de concorrência (N `Task.Run` simultâneos).

---

## Front admin (Vue) — `C:\ESTUDO\TruckFlowApp`

- `<script setup lang="ts">` sempre; `defineProps<{...}>()` e `defineEmits<{ evt: [payload: T] }>()` tipados; zero `any` em código novo.
- Cadeia por entidade: `src/entities/<x>.types.ts` → `src/services/<X>Service.ts` (factory `() => ({...})`, `http.<verbo><T>`) → `src/queries/<x>.queries.ts` (`useQuery`, key factory, `MaybeRef` + `unref`, `enabled` getter, `keepPreviousData`) → `src/hooks/use<X>.ts` (`useMutation` + invalidação + toast) → `src/components/Forms/<X>FormModal.vue` (emite `submit`) → `src/views/<X>View.vue`.
- Referência canônica: cadeia de **Usuario** (`ManageUserView.vue`).
- Pinia **só** UI/sessão. Dado de servidor só no Vue Query. Componente/view não importa service nem `http`.
- Listagem: paginação + filtros **server-side**, estado na URL (`route.query`), search com debounce ~350ms.
- `http.ts` já define `Content-Type` global e `withCredentials` — não passe headers manualmente.
- Auth: access token **em memória** (Pinia), refresh em cookie httpOnly, `restoreSession()` no bootstrap, single-flight refresh no 401.
- Toast em **milissegundos**. Mensagem do backend primeiro: `error.response?.data?.message ?? 'fallback'`.
- Rotas novas lazy com `meta: { requiresAuth: true, roles: RoleGroups.X }`; UI esconde ação com `usePermissions()`, mas a proteção real é no backend.
- Validar com `pnpm run build` (inclui `vue-tsc --build`).

## App motorista (Expo) — `C:\tf-mobile`

- Mesma cadeia: `src/Dtos/` → `src/services/` → `src/queries/` (key factory) → `src/hooks/` (mutations) → `app/(app)/...` (telas).
- Access token em memória (Zustand), refresh em **SecureStore `tf_refresh`**, axios em `src/services/http/axios.ts` com single-flight refresh.
- Zustand sempre com seletor; store só para sessão e wizard (nota fiscal em conferência).
- Tracking: foreground only, sem `ACCESS_BACKGROUND_LOCATION`, aviso LGPD antes da permissão, batch de posições (ADR-0003 + `Docs/rastreamento-motorista-backlog.md`).
- Validar com `npx tsc --noEmit` e `npx expo export --platform android`.

---

## Regras transversais

- **Edits cirúrgicos**: mude só o necessário; não remova logs, comentários ou código fora do escopo; não reformate arquivos inteiros.
- **ADRs antes de mexer em auth, notificações, tracking ou tenancy.** Mudou a decisão? Novo ADR que substitui o anterior — não edite o histórico.
- **Contrato entre repos**: mudança de DTO é aditiva por padrão; remoção/renomeação exige plano de compatibilidade com o mobile publicado.
- **Escala Aurora**: nada de estado em memória de uma instância (cache local, locks em memória, timers de domínio) sem plano para 2+ instâncias.
- **LGPD**: dado pessoal (telefone, CPF, localização) com finalidade, retenção e exclusão definidas; nunca em log.
- **Git**: commits e PRs **sem** linhas de atribuição ao Claude (`Co-Authored-By`, `Claude-Session`...). Branch por feature; `master` deploya em produção.
- **CLAUDE.md vivo**: quando algo descrito no `CLAUDE.md` de um repo for feito, mudado ou deixar de ser verdade, atualize-o na mesma entrega.
- Não criar arquivos `.md` de documentação sem pedido explícito.
