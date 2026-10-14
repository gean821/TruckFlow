---
name: dotnet-best-practices
description: Boas práticas de backend .NET 8 / ASP.NET Core / EF Core 9 para a API do TruckFlow (C:\ESTUDO\TruckFlow). 46 regras priorizadas por impacto — isolamento multi-tenant (IEmpresaScoped, EmpresaId da claim), hierarquia de exceções e envelope de erro, controllers magros, listagens paginadas, MapToResponse manual, FluentValidation, domínio rico com eventos via outbox, repositórios por agregado, xmin, DI por feature, HttpClient resiliente, workers multi-instância (SKIP LOCKED), testes com Postgres real e segurança (segredos, superfície, rate limit). Use ao escrever, revisar ou refatorar código C# da API, criar endpoint/service/entidade/worker ou revisar PR do backend.
version: 1.0.0
tags: [dotnet, aspnetcore, efcore, csharp, clean-architecture, multi-tenant, ddd, fluentvalidation, xunit, testcontainers]
---

# .NET Best Practices — TruckFlow API

Guia de padrões do backend do TruckFlow, ancorado no código real (`src/TruckFlowApi`) e nas decisões dos ADRs. Cada regra traz o porquê **neste** sistema (Aurora Alimentos, 21 fábricas concorrentes entre si, 2+ instâncias da API, clientes web + mobile publicado), exemplo incorreto/correto em C# no estilo da casa, onde o padrão já existe no repo, a dívida conhecida a **não copiar**, e a fonte.

## Quando usar

- Criar endpoint, service, DTO, validator, entidade, factory, repositório, worker ou extension de DI
- Revisar PR do backend (rode a seção CRITICAL como checklist)
- Investigar 500 inesperado, vazamento entre tenants, corrida de reserva, worker duplicando trabalho
- Antes de mudar design de auth/notificações/tracking: leia também `Docs/adr/*` (regra do CLAUDE.md)

Para SQL, índices, migrations, locks e plano de execução, use a skill **`postgres-best-practices`**. Para o agente que aplica tudo isso, **`dotnet-backend-specialist`**.

## Categorias por prioridade

| Prioridade | Categoria | Impacto | Prefixo | Regras |
|---|---|---|---|---|
| 1 | Isolamento multi-tenant | CRITICAL | `tenant-` | 6 |
| 2 | Erros e exceções | CRITICAL | `error-` | 5 |
| 3 | Contrato HTTP e controllers | HIGH | `api-` | 6 |
| 4 | Services e casos de uso | HIGH | `service-` | 6 |
| 5 | Modelo de domínio | HIGH | `domain-` | 5 |
| 6 | Acesso a dados (EF Core) | HIGH | `data-` | 5 |
| 7 | DI e configuração | MEDIUM | `di-` | 4 |
| 8 | Background e multi-instância | MEDIUM | `async-` | 3 |
| 9 | Testes | MEDIUM | `test-` | 3 |
| 10 | Segurança operacional | MEDIUM-HIGH | `security-` | 3 |

## Referência rápida — o fluxo da casa

```
Controller (magro, [Authorize(Roles = RoleGroups.X)], CancellationToken ct)
  → IXService (Application/Interfaces)
      1. ValidateAsync (FluentValidation) → throw ValidationException
      2. var empresaId = _currentUser.GetEmpresaId()        // CurrentUserGuard, nunca do DTO
      3. carregar relacionados → ?? throw new NotFoundException("... não encontrado.")
      4. XFactory.Create(dto, empresaId, ...)  /  entidade.MetodoDeDominio(...)  (DomainException + AddDomainEvent)
      5. repositório (await using tx se >1 SaveChanges) — catch xmin/23505/23P01 → BusinessException
      6. _logger.LogInformation("... {XId} (empresa {EmpresaId})", ...)
      7. return MapToResponse(entidade)                      // private static, DTO nunca entidade
  → ExceptionHandlingMiddleware → { success:false, code, message } | VALIDATION_ERROR errors[]
```

### Críticas (bloqueiam PR)

1. Entidade de tenant **implementa `IEmpresaScoped`** — `tenant-empresa-scoped-entity`
2. **`EmpresaId`/`MotoristaId` só da claim** — `tenant-empresaid-from-claim`
3. `IgnoreQueryFilters` só em `*AcrossTenants` + `WithTenant` — `tenant-ignore-query-filters`
4. `Usuario` checa `EmpresaId` na mão — `tenant-usuario-manual-check`
5. Exceção da hierarquia certa (nada vira 500 por engano) — `error-exception-hierarchy`
6. SqlState/xmin traduzidos em mensagem acionável — `error-map-postgres-sqlstate`
7. Teste cross-tenant para entidade nova — `test-cross-tenant-mandatory`
8. Segredo fora do repositório — `security-secrets-config`

### Padrões comuns

**Resolver tenant e falhar fechado:**
```csharp
var empresaId = _currentUser.GetEmpresaId();   // BusinessException se usuário sem empresa
```

**Not found honesto:**
```csharp
public Task<Grade?> GetById(Guid id, CancellationToken ct) => _db.Grade.FirstOrDefaultAsync(x => x.Id == id, ct);
var grade = await _repo.GetById(id, ct) ?? throw new NotFoundException("Grade não encontrada.");
```

**Listagem:**
```csharp
[HttpGet] public async Task<IActionResult> GetAll([FromQuery] XListQueryDto q, CancellationToken ct = default)
    => Ok(await _service.GetPaged(q, ct));      // PagedResponse<XResponse>
```

**Corrida:**
```csharp
catch (DbUpdateConcurrencyException) { throw new BusinessException("Registro alterado por outra pessoa. Atualize e tente de novo."); }
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }) { ... }
```

## Como usar

Leia a regra específica em `rules/<prefixo>-<nome>.md`. Cada arquivo tem:
- Por que importa no TruckFlow
- Exemplo **Incorreto** e **Correto** em C#
- **No TruckFlow:** onde o padrão existe e a dívida conhecida
- Referências (Microsoft Learn, RFCs, OWASP, autores)

Índice e descrições das seções: `rules/_sections.md`. Para criar regra nova: `rules/_template.md`.

Guia completo compilado (todas as regras num arquivo): `references/dotnet-guidelines.md`.

**Edits cirúrgicos**: as dívidas listadas em cada regra são para **não copiar** em código novo e para corrigir quando o escopo da tarefa tocar nelas — não para refatorar em massa de passagem.

## Fontes que embasam o guia

- Microsoft Learn — ASP.NET Core best practices, error handling/ProblemDetails, DI guidelines, hosted services, IHttpClientFactory, resilience, rate limiting, options; EF Core performance, concurrency, query filters, testing
- Microsoft — *Architecting Modern Web Applications with ASP.NET Core and Azure* e *.NET Microservices: Architecture* (eShopOnContainers): DDD, domain events, persistence layer
- Robert C. Martin — *Clean Architecture*; Jason Taylor e Steve "ardalis" Smith — Clean Architecture templates, Ardalis.Specification, GuardClauses
- Eric Evans — *Domain-Driven Design*; Vaughn Vernon — *Implementing DDD*; Martin Fowler — *PoEAA* (Repository, Unit of Work, DTO), *Anemic Domain Model*
- Vladimir Khorikov — *Unit Testing Principles, Practices, and Patterns*, "always-valid domain model"
- Jimmy Bogard — AutoMapper usage guidelines, vertical slices
- Jon P Smith — *Entity Framework Core in Action*
- Milan Jovanović — outbox, error handling, EF Core; Andrew Lock — DI, options, ASP.NET Core internals; Nick Chapsas
- David Fowler — *AspNetCoreDiagnosticScenarios* (async guidance); Stephen Cleary — async best practices
- Gregor Hohpe — *Enterprise Integration Patterns* (Idempotent Receiver); Chris Richardson — Transactional Outbox
- Nicholas Blumhardt / Serilog — structured logging
- OWASP API Security Top 10 (2023); RFC 9110 (HTTP Semantics); RFC 9457 (Problem Details); Stripe — idempotent requests
