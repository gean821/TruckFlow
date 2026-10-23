# .NET Best Practices — TruckFlow API (guia compilado)

> Gerado a partir de `rules/*.md`. Não edite à mão — edite a regra e recompile.

## Sumário

1. [Isolamento multi-tenant](#1-isolamento-multi-tenant) — **CRITICAL** (6 regras)
2. [Erros e exceções](#2-erros-e-exceções) — **CRITICAL** (5 regras)
3. [Contrato HTTP e controllers](#3-contrato-http-e-controllers) — **HIGH** (6 regras)
4. [Services e casos de uso](#4-services-e-casos-de-uso) — **HIGH** (6 regras)
5. [Modelo de domínio](#5-modelo-de-domínio) — **HIGH** (5 regras)
6. [Acesso a dados com EF Core](#6-acesso-a-dados-com-ef-core) — **HIGH** (5 regras)
7. [Injeção de dependência e configuração](#7-injeção-de-dependência-e-configuração) — **MEDIUM** (4 regras)
8. [Background services e concorrência entre instâncias](#8-background-services-e-concorrência-entre-instâncias) — **MEDIUM** (3 regras)
9. [Testes](#9-testes) — **MEDIUM** (3 regras)
10. [Segurança operacional](#10-segurança-operacional) — **MEDIUM-HIGH** (3 regras)

---

## 1. Isolamento multi-tenant

**Impacto:** CRITICAL  
**Descrição:** As 21 fábricas Aurora são marcas concorrentes dentro da mesma cooperativa (ADR-0001). Vazamento entre `Empresa`s é vazamento competitivo real e é o item nº 1 do OWASP API Top 10 (BOLA). Nenhuma outra categoria compensa um erro aqui.

### 1.1 ExecuteUpdate/ExecuteDelete e SQL cru não passam pelos interceptors

**Impacto:** CRITICAL — evita update em massa cross-tenant e buracos na trilha de auditoria e no outbox  
**Arquivo:** `rules/tenant-bulk-ops-bypass-interceptors.md`

`ExecuteUpdateAsync`/`ExecuteDeleteAsync` **respeitam** o global query filter (a query parte do `DbSet`), mas **não** passam pelo `SaveChanges` — logo `EmpresaScopedSaveChangesInterceptor`, `AuditSaveChangesInterceptor` e `OutboxSaveChangesInterceptor` não rodam. Com `IgnoreQueryFilters()` ou `ExecuteSql`/`FromSql`, nem o filtro existe. Use bulk só quando o ganho é real, filtre `EmpresaId` explicitamente e decida conscientemente o que acontece com auditoria e eventos.

**Incorreto:**

```csharp
await _db.Database.ExecuteSqlAsync(
    $"UPDATE \"Agendamento\" SET \"StatusAgendamento\" = 6 WHERE \"DataFim\" < {agora}", ct);
// todas as empresas, sem AuditLog, sem evento de domínio
```

**Correto:**

```csharp
var empresaId = _empresaContext.EmpresaId;
await _db.Agendamento
    .Where(a => a.EmpresaId == empresaId          // explícito, mesmo com filtro global
             && a.StatusAgendamento == StatusAgendamento.Disponivel
             && a.DataFim < agora)
    .ExecuteUpdateAsync(s => s
        .SetProperty(a => a.StatusAgendamento, StatusAgendamento.Expirado)
        .SetProperty(a => a.UpdatedAt, agora), ct);
```

Se a transição precisa de evento de domínio (notificar motorista), **não** use bulk: carregue em lotes (ex.: 500) e chame o método da entidade (`agendamento.Expirar()`), como `AgendamentoExpirationService` faz.

**No TruckFlow:** hot paths que vão usar SQL cru (upsert de `MotoristaPosicaoAtual`, ADR-0003) devem receber `EmpresaId` do contexto como parâmetro. `GradeRepositorio` usa `ExecuteDeleteAsync` — confira se o delete não deveria gerar auditoria.

Referência: [EF Core — ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete) · [EF Core Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)

### 1.2 Toda entidade de tenant implementa IEmpresaScoped

**Impacto:** CRITICAL — o global query filter e o interceptor de escrita passam a proteger a entidade sem código extra  
**Arquivo:** `rules/tenant-empresa-scoped-entity.md`

O isolamento do TruckFlow tem três peças que só funcionam juntas: o marcador `IEmpresaScoped`, o `HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)` aplicado por reflexão em `AppDbContext`, e o `EmpresaScopedSaveChangesInterceptor`, que preenche `EmpresaId` no Added e rejeita escrita em linha de outro tenant. Uma entidade nova com `EmpresaId` mas **sem** o marcador fica fora das três: qualquer `ToListAsync()` devolve dados de todas as fábricas.

**Incorreto (tem EmpresaId, mas não é escopada):**

```csharp
public class Transportadora : EntidadeBase
{
    public required string RazaoSocial { get; set; }
    public Guid EmpresaId { get; set; }   // ninguém filtra isso
    public Empresa? Empresa { get; set; }
}
```

**Correto:**

```csharp
public class Transportadora : EntidadeBase, IEmpresaScoped
{
    public required string RazaoSocial { get; set; }
    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
```

E na configuração, índice começando por `EmpresaId` (toda query filtra por ele):

```csharp
builder.HasIndex(x => new { x.EmpresaId, x.Cnpj }).IsUnique();
```

**No TruckFlow:** padrão em `TruckFlow.Domain/Contracts/IEmpresaScoped.cs`, aplicado a Agendamento, Grade, Fornecedor, Produto, NotaFiscal, Notificacao etc. Toda entidade nova escopada **precisa** de um teste cross-tenant (ver `test-cross-tenant-mandatory`).

Referência: [EF Core — Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters) · [OWASP API1:2023 BOLA](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)

### 1.3 EmpresaId (e MotoristaId) vem do JWT, nunca do body

**Impacto:** CRITICAL — fecha a classe inteira de bugs "usuário escolhe de qual empresa é o dado"  
**Arquivo:** `rules/tenant-empresaid-from-claim.md`

Se o DTO tem `EmpresaId`, algum dia alguém vai usá-lo. O tenant do request é **sempre** o da claim, resolvido por `CurrentUserGuard.GetEmpresaId()` (lança `BusinessException` se o usuário não tem empresa). O mesmo vale para `MotoristaId` em rotas do motorista (`GetMotoristaId()`) — ADR-0003 e ADR-0004 citam isso explicitamente para `/reservar` e `/motorista/posicao`.

**Incorreto:**

```csharp
public class AgendamentoAdminCreateDto
{
    public Guid EmpresaId { get; set; }   // campo existe "só por compatibilidade"
    // ...
}

var empresa = await _empresaRepo.GetById(dto.EmpresaId, ct); // confia no cliente
```

**Correto:**

```csharp
public async Task<GradeResponse> CreateGrade(GradeCreateDto dto, CancellationToken ct)
{
    var empresaId = _currentUser.GetEmpresaId();   // CurrentUserGuard
    // ...
    var grade = GradeFactory.Create(dto, empresaId, produto, fornecedor, descarga, descarga.UnidadeEntrega);
}
```

Se um fluxo legítimo precisa operar em outra empresa (superadmin, onboarding SaaS), ele tem endpoint próprio, role própria e validação explícita — não um campo opcional no DTO comum.

**No TruckFlow:** `GradeService` faz certo. Dívidas: `AuthAdminController.Register` é anônimo e aceita `dto.EmpresaId` do cliente (e `RegisterAdminAsync` não checa se a empresa voltou null); `AgendamentoAdminCreateDto.EmpresaId` existe e é ignorado — remover. Não resolva tenant com `_currentUser.EmpresaId ?? throw new UnauthorizedAccessException(...)` em um método e `?? throw new BusinessException(...)` em outro (como em `AgendamentoAdminService`): use o guard.

Referência: [OWASP API Security Top 10 2023 — API1 BOLA, API3 BOPLA](https://owasp.org/API-Security/editions/2023/en/0x11-t10/)

### 1.4 IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant

**Impacto:** CRITICAL — torna cada furo de isolamento explícito, nomeado e revisável em PR  
**Arquivo:** `rules/tenant-ignore-query-filters.md`

Há fluxos legítimos que cruzam tenant — o motorista não pertence a uma fábrica e reserva vaga em qualquer uma. O padrão é: o repositório expõe um método com sufixo `AcrossTenants` (único lugar com `IgnoreQueryFilters()`), e o service, logo após descobrir o tenant da linha, abre `using var scope = _empresaContext.WithTenant(x.EmpresaId)` para que o resto do fluxo (incluindo escrita e interceptors) rode no tenant correto.

**Incorreto (furo silencioso num método comum):**

```csharp
public Task<Agendamento?> GetById(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
```

**Correto:**

```csharp
// Repositório: o nome declara a intenção
public Task<Agendamento?> GetByIdAcrossTenants(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);

// Service
var vaga = await _repo.GetByIdAcrossTenants(dto.AgendamentoId, ct)
    ?? throw new NotFoundException("Vaga não encontrada.");
using var tenantScope = _empresaContext.WithTenant(vaga.EmpresaId);
// daqui pra baixo, filtros e interceptor usam vaga.EmpresaId
```

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment` e `ObterPorChaveAcrossTenantsAsync` (NotaFiscal) seguem isso. Em code review, `IgnoreQueryFilters` fora de método `*AcrossTenants` (ou de teste) é bloqueante.

Referência: [EF Core — Disabling filters](https://learn.microsoft.com/ef/core/querying/filters#disabling-filters)

### 1.5 Usuario não é IEmpresaScoped — cheque EmpresaId na mão

**Impacto:** CRITICAL — impede Admin da Empresa B de ler ou inativar usuário da Empresa A  
**Arquivo:** `rules/tenant-usuario-manual-check.md`

`Usuario` herda `IdentityUser<Guid>` e tem `EmpresaId` nulo (motorista não pertence a empresa), então **o global filter não se aplica**. `UserManager.FindByIdAsync(id)` devolve o usuário de qualquer tenant. Toda operação administrativa sobre usuário precisa comparar o `EmpresaId` do alvo com o do chamador — e responder **404** (não 403) para não confirmar existência.

**Incorreto:**

```csharp
public async Task SetAdminStatusAsync(Guid id, bool ativo, CancellationToken ct)
{
    var user = await _userManager.FindByIdAsync(id.ToString())
        ?? throw new NotFoundException("Usuário não encontrado.");
    user.DeletedAt = ativo ? null : DateTime.UtcNow;   // de qualquer empresa
    await _userManager.UpdateAsync(user);
}
```

**Correto:**

```csharp
var empresaId = _currentUser.GetEmpresaId();
var user = await _userManager.Users
    .FirstOrDefaultAsync(u => u.Id == id && u.EmpresaId == empresaId, ct)
    ?? throw new NotFoundException("Usuário não encontrado.");
```

**No TruckFlow:** dívida conhecida em `UsuarioService.SetAdminStatusAsync` e `GetAdminByIdAsync` (~L255-300). Cobrir com teste "Admin de B não altera usuário de A → NotFoundException".

Referência: [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity) · [OWASP API1:2023](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)

### 1.6 Workers iteram empresas e abrem WithTenant por empresa

**Impacto:** CRITICAL — background job não tem claim — sem WithTenant ele lança ou, pior, alguém "resolve" vendo tudo  
**Arquivo:** `rules/tenant-workers-with-tenant.md`

Fora de um request HTTP não existe claim `EmpresaId`. `EmpresaContext.EmpresaId` lança `UnauthorizedAccessException("Tenant não identificado.")` — comportamento certo (fail closed). O worker lista as empresas, abre `WithTenant(empresa.Id)` por iteração e isola a falha de uma empresa das demais.

**Incorreto:**

```csharp
// Sem tenant: alguém "resolve" com IgnoreQueryFilters e processa tudo misturado
var vencidos = await db.Agendamento.IgnoreQueryFilters()
    .Where(a => a.DataFim < agora).ToListAsync(ct);
```

**Correto:**

```csharp
foreach (var empresa in empresas)
{
    using var tenantScope = empresaContext.WithTenant(empresa.Id);
    try { await service.ExpirarVencidosAsync(stoppingToken); }
    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "Falha ao expirar agendamentos da empresa {EmpresaId}. Próximas empresas seguem.",
            empresa.Id);
    }
}
```

Eventos de outbox carregam `EmpresaId` e o handler roda sob `WithTenant(outboxEvent.EmpresaId)`.

**No TruckFlow:** `TruckFlow/Extensions/Agendamento/AgendamentoExpirationWorker.cs` e `OutboxProcessorWorker` são a referência. ADR-0001/0004 exigem que contexto sem tenant resulte em **zero linhas** ou exceção, nunca em "sem filtro".

Referência: [Background tasks with hosted services](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services)


---

## 2. Erros e exceções

**Impacto:** CRITICAL  
**Descrição:** Admin Vue e app mobile leem `response.data.message` para mostrar ao operador. Exceção no tipo errado vira 500 genérico; exceção engolida vira dado inconsistente; stack trace em produção vira achado de pen-test.

### 2.1 Conflito de concorrência merece 409, não 400 (evolução)

**Impacto:** MEDIUM — cliente distingue "dado inválido" de "alguém chegou antes" e reage (refetch automático)  
**Arquivo:** `rules/error-conflict-409.md`

Hoje toda corrida (xmin, 23505, 23P01) sai como `BusinessException` → 400. Funciona, mas o front/mobile não consegue diferenciar "você digitou errado" de "a vaga acabou de ser pega" — e no segundo caso o certo é **refazer a query** automaticamente. Uma `ConflictException` mapeada para **409** com `code` próprio resolve sem quebrar quem só lê `message`.

**Incorreto (tudo é 400):**

```csharp
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista.");
}
```

**Correto:**

```csharp
public class ConflictException(string message)
    : HttpResponseException("CONFLICT", message) { }

// middleware
ConflictException => StatusCodes.Status409Conflict,

catch (DbUpdateConcurrencyException)
{
    throw new ConflictException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

Cliente (Vue Query / React Query): `onError` com `status === 409` → `invalidateQueries` da lista + toast.

Introduza por endpoint, começando por `/reservar`, e avise os agentes de front/mobile na mesma PR.

**No TruckFlow:** candidato natural em `AgendamentoMotoristaService.BookAppointment` e `AgendamentoAdminService`.

Referência: [RFC 9110 §15.5.10 — 409 Conflict](https://www.rfc-editor.org/rfc/rfc9110#name-409-conflict) · [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines)

### 2.2 Lance a exceção certa da hierarquia — e só ela vira 4xx

**Impacto:** CRITICAL — 500 genérico vira 400/404 com mensagem que o operador entende  
**Arquivo:** `rules/error-exception-hierarchy.md`

O `ExceptionHandlingMiddleware` só converte em 4xx o que conhece. A hierarquia é:

| Exceção | Onde lançar | HTTP | `code` |
|---|---|---|---|
| `NotFoundException` (`HttpResponseException`) | service, registro não existe **no tenant** | 404 | `NOT_FOUND` |
| `BusinessException` (`HttpResponseException`) | service, regra de caso de uso violada | 400 | `BUSINESS_ERROR` |
| `DomainException` (Domain) | entidade, invariante/transição inválida | 400 | `BUSINESS_ERROR` |
| `FluentValidation.ValidationException` | service, após `ValidateAsync` | 400 | `VALIDATION_ERROR` + `errors[]` |
| `UnauthorizedAccessException` | guard/contexto sem usuário ou tenant | 401 | — |
| qualquer outra | — | 500 | mensagem genérica em prod |

Qualquer exceção fora dessa lista é **500** — inclusive `BadRequestException`, que hoje herda `Exception` direto.

**Incorreto:**

```csharp
public class BadRequestException : Exception   // middleware não conhece → 500
{
    public BadRequestException(string message) : base(message) { }
}

if (user is null) throw new Exception("Usuário não encontrado");   // 500
```

**Correto:**

```csharp
public class BadRequestException(string message)
    : HttpResponseException("BAD_REQUEST", message) { }

// e no switch do middleware:
BadRequestException => StatusCodes.Status400BadRequest,

var user = await _repo.GetById(id, ct)
    ?? throw new NotFoundException("Usuário não encontrado.");
```

Regras práticas:
- Invariante da entidade (transição de status) → `DomainException` dentro da entidade. Regra que depende de outras entidades/repositórios → `BusinessException` no service.
- Mensagem em **PT-BR, para o usuário final**, dizendo o que fazer ("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.").
- Nova exceção = herdar de `HttpResponseException` com `code` próprio **e** mapear no middleware na mesma PR.

**No TruckFlow:** `TruckFlow.Application/Exceptions/*` e `TruckFlow/Middlewares/ExceptionHandlingMiddleware.cs`. Dívidas: `BadRequestException` → 500 (~8 usos em `UsuarioService`); `TruckFlow/Exceptions/DomainException.cs` duplicado e sem uso.

Referência: [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) · [Milan Jovanović — Global error handling in ASP.NET Core 8](https://www.milanjovanovic.tech/blog/global-error-handling-in-aspnetcore-8)

### 2.3 Traduza SqlState do Postgres e DbUpdateConcurrencyException em BusinessException

**Impacto:** CRITICAL — corrida de reserva vira mensagem acionável em vez de 500  
**Arquivo:** `rules/error-map-postgres-sqlstate.md`

No TruckFlow a **última linha de defesa contra corrida é o banco** (xmin, exclusion constraint, unique filtrado). A checagem prévia na aplicação (`GetConflitosAsync`) dá mensagem amigável no caso comum, mas a garantia vem do Postgres — e o erro dele precisa virar `BusinessException` com texto útil. Sem isso, o motorista vê "Erro interno" quando na verdade alguém pegou a vaga 50ms antes.

| SqlState / exceção | Significado | Mensagem sugerida |
|---|---|---|
| `DbUpdateConcurrencyException` | `xmin` mudou (otimista) | "Registro foi alterado por outra pessoa. Atualize e tente de novo." |
| `23505` unique_violation | duplicado | "Já existe ... com esses dados." |
| `23P01` exclusion_violation | sobreposição de horário na doca | "Conflito de horário nesta doca." |
| `23503` foreign_key_violation | referência inexistente / em uso | "Não é possível excluir: ... está em uso." |
| `40001` serialization_failure | isolamento Serializable | retry com backoff (não exponha ao usuário) |

**Incorreto:**

```csharp
await _repo.Update(vaga, ct);   // estoura DbUpdateException → 500
```

```csharp
catch (Exception) { throw new BusinessException("Erro ao reservar."); } // engole a causa
```

**Correto (padrão de `AgendamentoMotoristaService.BookAppointment`):**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
try
{
    await _repo.Update(vaga, ct);
    await _lifecycle.AoReservarAsync(vaga, pesoEstimado, ct);
    await tx.CommitAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException(
        "Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
{
    throw new BusinessException("Conflito de horário detectado para esta doca.");
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
{
    throw new BusinessException("Esta vaga já foi reservada por outro motorista neste instante.");
}
```

Prefira as constantes `PostgresErrorCodes.*` do Npgsql a strings mágicas. Se o padrão se repetir em mais serviços, extraia um helper (`PostgresErrors.IsUniqueViolation(ex, "nome_constraint")`) — checando o **nome da constraint** (`pg.ConstraintName`) quando a tabela tem mais de uma unique.

**No TruckFlow:** repetido em `AgendamentoAdminService` (Create/Update/Cancelar/Finalizar) e `GradeService.CreateGrade`. Lacuna: `DbUpdateConcurrencyException` não tratada no service vira 500 pelo middleware.

Referência: [Npgsql — PostgresErrorCodes](https://www.npgsql.org/doc/api/Npgsql.PostgresErrorCodes.html) · [PostgreSQL Error Codes](https://www.postgresql.org/docs/current/errcodes-appendix.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)

### 2.4 Não engula exceção — trate, traduza ou deixe subir

**Impacto:** CRITICAL — evita estado inconsistente silencioso (reserva sem evento, nota sem item)  
**Arquivo:** `rules/error-no-swallow.md`

Um `catch` só se justifica se faz uma de três coisas: **recupera** (retry, fallback documentado), **traduz** (Postgres → `BusinessException`) ou **enriquece e relança** (log com contexto + `throw;`). `catch { }` ou `catch (Exception) { return null; }` esconde bug de produção e, num sistema fiscal, gera dado que ninguém consegue auditar depois.

**Incorreto:**

```csharp
try { await _sefaz.ConsultarDistribuicaoAsync(chave, ct); }
catch (Exception) { return null; }   // SEFAZ fora? certificado vencido? ninguém sabe

try { await _push.SendAsync(msg, ct); }
catch { }                            // e o NotificacaoEntrega fica "Pendente" para sempre
```

**Correto:**

```csharp
try
{
    return await _sefaz.ConsultarDistribuicaoAsync(chave, ct);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogWarning(ex, "SEFAZ indisponível ao buscar nota {ChaveFinal}", chave[^6..]);
    throw new BusinessException("SEFAZ indisponível no momento. Envie o XML da nota ou tente em alguns minutos.");
}
```

Para relançar a mesma exceção use `throw;` (preserva stack), nunca `throw ex;`.

Exceções toleradas de "best effort" (ex.: `signOut` que tenta revogar refresh token) devem **logar** em `Warning` e ter comentário dizendo por que a falha é aceitável.

**No TruckFlow:** workers (`OutboxProcessorWorker`, `AgendamentoExpirationWorker`) fazem certo: capturam por item/empresa, logam com contexto, seguem. `DbUpdateConcurrencyException` no `AgendamentoExpirationService` é logado e deixado para o próximo ciclo — exemplo de "recuperar" bem documentado.

Referência: [Stephen Cleary — exception handling in async](https://blog.stephencleary.com/2012/02/async-and-await.html) · [.NET — Best practices for exceptions](https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions)

### 2.5 Um formato de erro só — unifique o 400 automático do [ApiController]

**Impacto:** HIGH — front e mobile param de tratar dois (três) formatos de erro  
**Arquivo:** `rules/error-unified-envelope.md`

Hoje existem formatos concorrentes:
- middleware: `{ "success": false, "code": "BUSINESS_ERROR", "message": "..." }`
- validação FluentValidation: `{ "success": false, "code": "VALIDATION_ERROR", "errors": ["..."] }`
- `[ApiController]` em falha de model binding (Guid inválido, JSON malformado): `ValidationProblemDetails` padrão do ASP.NET
- `return BadRequest("Chave de acesso inválida.")` no controller: **string crua**

Os clientes leem `response.data.message`. Os dois últimos formatos chegam como `undefined` e o usuário vê só o fallback.

**Incorreto:**

```csharp
if (chave.Length != 44) return BadRequest("Chave de acesso inválida.");   // string crua
```

**Correto (curto prazo — tudo no envelope atual):**

```csharp
// Program.cs / extension de Controllers
builder.Services.AddControllers(o => o.Filters.Add<ActionLoggingFilter>())
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx => new BadRequestObjectResult(new
        {
            success = false,
            code = "VALIDATION_ERROR",
            message = "Requisição inválida.",
            errors = ctx.ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
        });
    });

// controller: validação vai para o service (FluentValidation) — controller não valida
```

Inclua sempre `message` também no `VALIDATION_ERROR` (primeiro erro ou resumo) — o front mostra `message`.

**Evolução (registrar em ADR antes):** migrar para **ProblemDetails (RFC 9457)** com `AddProblemDetails()` + `IExceptionHandler` (.NET 8), mantendo `code` como extension member e `title`/`detail` em PT-BR. Faça isso de uma vez para web **e** mobile, com período de compatibilidade (mobile publicado não atualiza junto).

**No TruckFlow:** dívida em `AgendamentoMotoristaController` (`return BadRequest("...")`). Erros em produção nunca devolvem stack (já correto no middleware; mantenha).

Referência: [RFC 9457 — Problem Details for HTTP APIs](https://www.rfc-editor.org/rfc/rfc9457) · [ASP.NET Core — IExceptionHandler e ProblemDetails](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling#iexceptionhandler) · [Andrew Lock — Problem details in .NET 8](https://andrewlock.net/)


---

## 3. Contrato HTTP e controllers

**Impacto:** HIGH  
**Descrição:** Controllers magros, rotas versionadas, status codes corretos, listagens paginadas e idempotência. É o contrato consumido por dois clientes (web + mobile publicado em loja), então mudança aqui é cara.

### 3.1 CancellationToken como último parâmetro, propagado até o EF

**Impacto:** MEDIUM — request abortado (motorista perdeu sinal, usuário trocou de página) libera conexão do pool  
**Arquivo:** `rules/api-cancellation-token.md`

O ASP.NET injeta `HttpContext.RequestAborted` em qualquer `CancellationToken` de action. Se ele não chega até `ToListAsync(ct)`, a query continua rodando no Postgres depois que o cliente desistiu — e com 21 fábricas e pool limitado (PgBouncer, ADR-0004 item 8) isso é conexão desperdiçada.

Convenção para código novo: nome **`ct`**, último parâmetro, sem `= default` em services/repos (obriga quem chama a pensar); `= default` tolerado só em controllers.

**Incorreto:**

```csharp
public async Task<IActionResult> GetAll([FromQuery] GradeListQueryDto query)
    => Ok(await _service.GetPagedGrades(query));          // token não existe

public Task<List<Grade>> GetAll() => _db.Grade.ToListAsync();   // repo ignora cancelamento
```

**Correto:**

```csharp
public async Task<IActionResult> GetAll([FromQuery] GradeListQueryDto query, CancellationToken ct = default)
    => Ok(await _service.GetPagedGrades(query, ct));

public Task<PagedResponse<Grade>> GetPagedAsync(GradeListQueryDto query, CancellationToken ct)
    => /* ... */ dbQuery.ToListAsync(ct);
```

Não passe o token de request para trabalho que **deve** completar mesmo se o cliente cair (ex.: gravar outbox após commit) — aí use `CancellationToken.None` com comentário.

**No TruckFlow:** nomes hoje variam (`token`, `ct`, `cancellationToken`) — não renomeie em massa (edit cirúrgico), só padronize em código novo. `AgendamentoAdminController.UpdateAgendamento` não recebe token.

Referência: [David Fowler — AspNetCoreDiagnosticScenarios: Async guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md) · [ASP.NET Core best practices](https://learn.microsoft.com/aspnet/core/fundamentals/best-practices)

### 3.2 Idempotency-Key em POSTs que o cliente pode repetir

**Impacto:** HIGH — retry de rede do mobile não duplica reserva nem posição  
**Arquivo:** `rules/api-idempotency-key.md`

Motorista no pátio com 3G: o `POST /reservar` sai, o servidor processa, a resposta se perde, o app (ou o usuário) tenta de novo. Sem idempotência, o segundo request ou cria uma segunda escrita ou devolve um erro falso ("vaga já reservada" — por ele mesmo). ADR-0004 item 12 torna isso pré-piloto para `/reservar` e `POST /motorista/posicao`.

Padrão (Stripe): cliente gera um UUID por **intenção** (não por tentativa) e envia `Idempotency-Key`. Servidor grava `(EmpresaId|MotoristaId, Key, RequestHash, StatusCode, ResponseBody, CriadoEm)` com unique em `(Escopo, Key)`, dentro da mesma transação do efeito. Repetição com mesma chave e mesmo hash → devolve a resposta salva; mesma chave com payload diferente → 422. Retenção 24h.

**Incorreto:**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar([FromBody] ReservarAgendamentoDto dto, CancellationToken ct)
    => Ok(await _service.BookAppointment(dto, ct));    // retry = segunda tentativa de reserva
```

**Correto (esboço):**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar(
    [FromBody] ReservarAgendamentoDto dto,
    [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
    CancellationToken ct)
{
    var result = await _service.BookAppointment(dto, idempotencyKey, ct);
    return Ok(result);
}

// service
if (idempotencyKey is { } key)
{
    var anterior = await _idempotencia.ObterAsync(_currentUser.GetMotoristaId(), key, ct);
    if (anterior is not null) return anterior.Deserialize<AgendamentoMotoristaResponse>();
}
// ... efeito + gravação da chave na MESMA transação; 23505 na chave = corrida com a própria repetição → reler e devolver
```

Prefira tabela no Postgres a Redis enquanto não houver Redis na stack (menos uma peça para a Aurora auditar). Comece opcional (header ausente = comportamento atual) para não quebrar o mobile já publicado.

**No TruckFlow:** domain events já carregam `IdempotencyKey` e o lifecycle tem "reserva idempotente" — reaproveite a ideia na borda HTTP.

Referência: [Stripe — Idempotent requests](https://docs.stripe.com/api/idempotent_requests) · [IETF draft — The Idempotency-Key HTTP Header Field](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) · [Gregor Hohpe — Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)

### 3.3 Toda listagem de tela nasce paginada — XListQueryDto → PagedResponse<T>

**Impacto:** HIGH — lista de 21 fábricas não derruba banco nem front; filtros server-side  
**Arquivo:** `rules/api-paged-listing.md`

Convenção arquitetural do projeto: nenhuma rota de listagem retorna `List<T>` cru, mesmo que hoje tenha 10 registros. Entrada `[FromQuery] XListQueryDto` com `PageNumber`, `PageSize` e os filtros (`Search`, status, datas, IDs); saída `PagedResponse<TResponse>` (`Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`). O front sincroniza esses parâmetros com a URL e faz search com debounce **server-side**.

Exceções aceitas: `GetById`, dropdowns curtos/enums, endpoints internos.

**Incorreto:**

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(CancellationToken ct)
    => Ok(await _service.GetAll(ct));     // List<FornecedorResponse> com tudo
```

**Correto:**

```csharp
public class TransportadoraListQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? Ativo { get; set; }
}

[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] TransportadoraListQueryDto query, CancellationToken ct = default)
    => Ok(await _service.GetPaged(query, ct));

// repositório
var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
var pageSize = Math.Clamp(query.PageSize, 1, 100);        // teto evita pageSize=100000
var totalCount = await dbQuery.CountAsync(ct);
var items = await dbQuery
    .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)   // ordenação determinística
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new TransportadoraResponse { Id = x.Id, RazaoSocial = x.RazaoSocial, Cnpj = x.Cnpj })
    .ToListAsync(ct);
```

Sempre **ordenação estável** (desempate por `Id`) — senão itens pulam/repetem entre páginas. Aplique **teto** em `PageSize` (o export CSV do front pede 1000 hoje — export deve ter endpoint próprio). Para tabelas que crescem sem limite (posições, audit), considere keyset pagination (ver `postgres-best-practices/data-pagination`).

**No TruckFlow:** referência `PlanejamentoListQueryDto` + `PlanejamentoRecebimentoService.GetPaged`, `GradeListQueryDto` + `GradeRepositorio.GetPagedAsync`. Fornecedor, Produto, UnidadeEntrega e LocalDescarga ainda retornam lista crua (dívida).

Referência: [EF Core — Pagination](https://learn.microsoft.com/ef/core/querying/pagination) · [Microsoft REST API Guidelines — collections](https://github.com/microsoft/api-guidelines)

### 3.4 Autorização com RoleGroups/Roles constantes, nunca string literal

**Impacto:** HIGH — renomear/adicionar papel muda um lugar; typo não vira endpoint aberto  
**Arquivo:** `rules/api-role-groups.md`

Papéis reais: `Admin`, `Gerente`, `Monitor`, `Portaria`, `Motorista` (`TruckFlow.Domain/Entities/Roles.cs`). Grupos de permissão em `RoleGroups` (`CanManageGrade`, `CanCheckIn`, `CanViewSchedule`, `CanManageUsers`, `CanManageMasterData`, `CanViewAuditLogs`) — espelhados no front em `src/shared/auth/roles.ts`. Atributo na classe com o caso geral, override na action quando necessário. Endpoint sem `[Authorize]` precisa de `[AllowAnonymous]` **explícito** e justificativa.

**Incorreto:**

```csharp
[Authorize(Roles = "Motorista")]                 // literal
[Authorize(Roles = "Admin,Operador")]            // "Operador" não existe → ninguém acessa (ou alguém "conserta" removendo)
public class AgendamentoMotoristaController : ControllerBase { }
```

**Correto:**

```csharp
[Authorize(Roles = RoleGroups.CanViewSchedule)]
public sealed class AgendamentoAdminController(...) : ControllerBase
{
    [HttpPatch("{agendamentoId:guid}/check-in")]
    [Authorize(Roles = RoleGroups.CanCheckIn)]
    public async Task<IActionResult> CheckIn([FromRoute] Guid agendamentoId, CancellationToken ct = default)
    {
        await _service.RegistrarChegadaAsync(agendamentoId, ct);
        return NoContent();
    }
}

[Authorize(Roles = Roles.Motorista)]
public sealed class AgendamentoMotoristaController(...) : ControllerBase { }
```

Novo grupo de permissão = adicionar em `RoleGroups` (backend) **e** `roles.ts` (front) na mesma feature. Role diz *quem pode chamar*; **tenant** (`EmpresaId`) diz *em quais dados* — as duas checagens são independentes e obrigatórias.

**No TruckFlow:** dívida `AgendamentoMotoristaController` com literal `"Motorista"`. Entra ID mapeia grupos AD → esses mesmos papéis (`EntraGroupRoleMapping`); motorista nunca vem do Entra.

Referência: [Role-based authorization in ASP.NET Core](https://learn.microsoft.com/aspnet/core/security/authorization/roles) · [OWASP API5:2023 Broken Function Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa5-broken-function-level-authorization/)

### 3.5 Rotas v1, status codes corretos e contrato documentado

**Impacto:** HIGH — clientes web e mobile recebem semântica HTTP previsível e Swagger fiel  
**Arquivo:** `rules/api-routes-and-results.md`

- Base: `[Route("v1/[Controller]")]` ou rota explícita em kebab/plural (`v1/usuarios`, `v1/recebimentos`). **Sem barra inicial** (`/v1/...` ignora prefixos e já causou inconsistência).
- Constraint de tipo em parâmetros de rota: `{id:guid}` — rota com Guid malformado vira 404, não 400 de binding.
- `POST` que cria → `CreatedAtAction(nameof(GetById), new { id = x.Id }, x)`. `PATCH`/`DELETE` sem corpo de retorno → `NoContent()`. `GET` → `Ok(dto)`.
- Ações de estado são sub-recursos com verbo explícito: `PATCH v1/AgendamentoAdmin/{id:guid}/check-in`.
- Hoje todos retornam `Task<IActionResult>`. Em código novo, prefira `Task<ActionResult<T>>` (ou declare `[ProducesResponseType<T>(200)]`) para o Swagger mostrar o schema — o front gera tipos a partir dele.

**Incorreto:**

```csharp
[Route("/v1/[Controller]")]
public class PlanejamentoRecebimentoController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(PlanejamentoCreateDto dto)
        => Ok(await _service.Create(dto));          // 200 em criação, sem Location
}
```

**Correto:**

```csharp
[ApiController]
[Route("v1/[Controller]")]
[Authorize(Roles = RoleGroups.CanManageGrade)]
public sealed class TransportadoraController(ITransportadoraService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TransportadoraResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<TransportadoraResponse>> Create(
        [FromBody] TransportadoraCreateDto dto, CancellationToken ct)
    {
        var criada = await service.Create(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = criada.Id }, criada);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransportadoraResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
        => Ok(await service.GetById(id, ct));
}
```

**No TruckFlow:** `GradeController` (CreatedAtAction/NoContent certos). Dívidas: `AgendamentoAdmin` create retorna `Ok`; `PlanejamentoRecebimentoController` com `/v1`; nomes de arquivo ≠ classe (`RecebimentoController.cs` contém `RecebimentosController`). Mudança de rota existente quebra o mobile publicado — só com período de compatibilidade.

Referência: [Controller action return types](https://learn.microsoft.com/aspnet/core/web-api/action-return-types) · [Microsoft REST API Guidelines](https://github.com/microsoft/api-guidelines) · [RFC 9110 — HTTP Semantics](https://www.rfc-editor.org/rfc/rfc9110)

### 3.6 Controller magro — recebe, delega ao service, devolve

**Impacto:** HIGH — regra de negócio testável sem HTTP e um único lugar para validação/tenant  
**Arquivo:** `rules/api-thin-controllers.md`

O controller é adaptador HTTP: binding, autorização por atributo, chamada do service, status code. **Não** valida payload (FluentValidation roda no service), **não** lê claim (o service usa `CurrentUserGuard`), **não** acessa repositório, **não** faz `if (x == null) return NotFound()` (o service lança `NotFoundException`).

**Incorreto:**

```csharp
[HttpPut("{id}")]
public async Task<IActionResult> UpdateAgendamento(Guid id, AgendamentoAdminUpdateDto dto)
{
    if (dto.DataFim <= dto.DataInicio) return BadRequest("Datas inválidas");   // validação no controller
    var empresaId = Guid.Parse(User.FindFirst("EmpresaId")!.Value);             // tenant no controller
    var agendamento = await _service.Update(id, dto, empresaId);
    if (agendamento == null) return NotFound();                                // código morto
    return Ok(agendamento);
}
```

**Correto (padrão `GradeController`):**

```csharp
[HttpPatch("{id:guid}")]
public async Task<IActionResult> Update(
    [FromRoute] Guid id,
    [FromBody] GradeUpdateDto dto,
    CancellationToken ct)
{
    var grade = await _service.UpdateGrade(id, dto, ct);
    return Ok(grade);
}
```

Primary constructor (`GradeController(IGradeService service) : ControllerBase`) e `sealed` são bem-vindos em controllers novos.

**No TruckFlow:** referência `TruckFlow/Controllers/GradeController.cs`. Dívidas: `AgendamentoAdminController.UpdateAgendamento` (sem CancellationToken, `NotFound` morto), `PlanejamentoRecebimentoController.GetById` (mesmo `NotFound` morto), validação inline em `AgendamentoMotoristaController`. Exceção conhecida e aceitável: `NotificationsStreamController` injeta `ICurrentUserService` porque é streaming.

Referência: [Robert C. Martin — Clean Architecture (adapters)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) · [Microsoft — Architecting Modern Web Apps with ASP.NET Core](https://learn.microsoft.com/dotnet/architecture/modern-web-apps-azure/)


---

## 4. Services e casos de uso

**Impacto:** HIGH  
**Descrição:** Onde mora a orquestração: validação, resolução de tenant, transação, mapeamento para DTO e log. Services retornam DTOs, nunca entidades EF.

### 4.1 Valide no service com FluentValidation e lance ValidationException

**Impacto:** HIGH — uma única porta de entrada para regras de payload, com mensagens PT-BR e formato VALIDATION_ERROR  
**Arquivo:** `rules/service-fluent-validation.md`

O projeto usa FluentValidation 12 **chamado manualmente** no início do método de service (sem auto-validation — o pacote de auto-validação do MVC está descontinuado pelo próprio FluentValidation). O validator é registrado por DTO na extension da feature. O middleware converte `ValidationException` em `400 { code: "VALIDATION_ERROR", errors: [...] }`.

Separação de responsabilidades:
- **Validator**: forma do payload (obrigatório, tamanho, formato de CNPJ/chave NF-e de 44 dígitos, `DataFim > DataInicio`). Sem I/O.
- **Service**: regras que precisam de banco (produto existe *no tenant*, doca livre) → `NotFoundException`/`BusinessException`.
- **Entidade**: invariantes de estado → `DomainException`.

**Incorreto:**

```csharp
public async Task<GradeResponse> CreateGrade(GradeCreateDto dto, CancellationToken ct)
{
    if (string.IsNullOrEmpty(dto.Nome)) throw new BusinessException("Nome obrigatório"); // espalhado
    // ...
}

// validator que faz I/O
RuleFor(x => x.ProdutoId).MustAsync(async (id, ct) => await _repo.Exists(id, ct));
```

**Correto (padrão `GradeService.CreateGrade`):**

```csharp
var validation = await _createValidator.ValidateAsync(dto, ct);
if (!validation.IsValid)
{
    _logger.LogWarning("Validação falhou ao criar grade: {Errors}",
        string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
    throw new ValidationException(validation.Errors);
}
var empresaId = _currentUser.GetEmpresaId();
var produto = await _produtoRepo.GetById(dto.ProdutoId, ct)
    ?? throw new NotFoundException("Produto não encontrado.");
```

```csharp
public sealed class TransportadoraCreateValidator : AbstractValidator<TransportadoraCreateDto>
{
    public TransportadoraCreateValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().WithMessage("Informe a razão social.")
            .MaximumLength(200).WithMessage("Razão social deve ter até 200 caracteres.");
        RuleFor(x => x.Cnpj).Matches(@"^\d{14}$").WithMessage("CNPJ deve ter 14 dígitos.");
    }
}

// Extensions/Transportadora/TransportadoraDependencyInjection.cs
services.AddScoped<IValidator<TransportadoraCreateDto>, TransportadoraCreateValidator>();
```

Namespace = pasta (`TruckFlow.Application.Validators.<Feature>`), classe `XCreateValidator` no arquivo `XCreateValidator.cs`.

**No TruckFlow:** `TruckFlow.Application/Validators/<Feature>/`. Dívidas: `AgendamentoAdminCreateValidator` em namespace `...AgendamentoMotorista`; arquivo `AgendamentoAdminUpdateValidator.cs` com classe `AgendamentoAdminUpdateDtoValidator`.

Referência: [FluentValidation — ASP.NET Core integration (manual validation recomendada)](https://docs.fluentvalidation.net/en/latest/aspnet.html) · [Vladimir Khorikov — Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)

### 4.2 Service devolve DTO, mapeado à mão com private static MapToResponse

**Impacto:** HIGH — contrato estável, sem lazy-load acidental nem vazamento de campo interno, checado em compile-time  
**Arquivo:** `rules/service-map-to-response.md`

Entidade EF fora do service vaza navegação (ciclo na serialização, N+1 por lazy load), campos internos (`xmin`, `CreatedBy`, hash de senha do Identity) e acopla o contrato público ao schema. A convenção da casa: **toda** assinatura pública de service retorna `XResponse`/`XResponseDto` (ou `PagedResponse<XResponse>`), e a conversão é um `private static XResponse MapToResponse(Entity e)` no fim do service.

Por que mapeamento manual e não AutoMapper: o compilador pega campo renomeado, "Find usages" funciona, não há reflexão nem convenção mágica escondendo `null`. O próprio autor do AutoMapper (Jimmy Bogard) diz que ele **não** foi feito para mapear para DTO com lógica (fallbacks, enum → string). Se o volume de mapeamento crescer muito, a evolução aceitável é **Mapperly** (source generator, zero reflexão) — com ADR, não ad hoc.

**Incorreto:**

```csharp
public async Task<Agendamento> GetById(Guid id, CancellationToken ct)
    => await _repo.GetById(id, ct);                       // entidade EF no contrato
```

**Correto (padrão `AgendamentoAdminService`):**

```csharp
public async Task<AgendamentoAdminResponse> GetById(Guid id, CancellationToken ct)
{
    var agendamento = await _repo.GetById(id, ct)
        ?? throw new NotFoundException("Agendamento não encontrado.");
    return MapToResponse(agendamento);
}

private static AgendamentoAdminResponse MapToResponse(Agendamento agendamento) => new()
{
    Id = agendamento.Id,
    FornecedorNome = agendamento.Fornecedor?.Nome,
    MotoristaNome = agendamento.Usuario?.Motorista?.NomeReal,
    DataInicio = agendamento.DataInicio,
    DataFim = agendamento.DataFim,
    PesoCarga = agendamento.VolumeCarga ?? agendamento.NotaFiscal?.PesoBruto ?? 0,
    PlacaVeiculo = agendamento.PlacaVeiculo ?? agendamento.NotaFiscal?.PlacaVeiculo,
    Status = agendamento.StatusAgendamento.ToString(),
    CreatedAt = agendamento.CreatedAt,
};
```

Regras do DTO de saída: só o que a tela precisa; enum como string quando o front exibe; telefone já sanitizado (só dígitos com DDI, ADR-0002); nada de `EmpresaId` de outro tenant; `MapToResponse` puro (sem I/O, sem `DateTime.Now`).

Nomenclatura para código novo: `XCreateDto`, `XUpdateDto`, `XListQueryDto`, `XResponse` (é o mais comum hoje). Não renomeie os `*ResponseDto` existentes por estética.

**No TruckFlow:** 13 services com `MapToResponse`. Referência: `TruckFlow.Application/AgendamentoAdminService.cs` (L460-481), `GradeService`.

Referência: [Jimmy Bogard — AutoMapper usage guidelines](https://www.jimmybogard.com/automapper-usage-guidelines/) · [Mapperly](https://mapperly.riok.app/) · [Martin Fowler — Data Transfer Object](https://martinfowler.com/eaaCatalog/dataTransferObject.html)

### 4.3 Leitura paginada projeta direto para o DTO com Select

**Impacto:** HIGH — busca só as colunas da tela; elimina Include em cascata e tracking em listagens  
**Arquivo:** `rules/service-projection-paged-reads.md`

Para **escrita**, carregue a entidade (com os `Include` necessários) — você vai chamar método de domínio nela. Para **listagem**, projete: `.Select(x => new XResponse { ... })` traduz para um `SELECT` só das colunas usadas, com `JOIN` para as navegações acessadas, sem tracking e sem materializar o grafo. Numa listagem de agendamentos com 6 `Include`, a diferença é grande em I/O e memória.

**Incorreto (listagem carregando o grafo inteiro):**

```csharp
var items = await _db.Agendamento
    .Include(x => x.Fornecedor).Include(x => x.Usuario).ThenInclude(u => u!.Motorista)
    .Include(x => x.NotaFiscal).Include(x => x.LocalDescarga).Include(x => x.Grade).ThenInclude(g => g!.Produto)
    .Skip(skip).Take(pageSize)
    .ToListAsync(ct);                    // tracking + todas as colunas de 7 tabelas
return items.Select(MapToResponse).ToList();
```

**Correto:**

```csharp
var items = await dbQuery
    .OrderBy(x => x.DataInicio).ThenBy(x => x.Id)
    .Skip(skip).Take(pageSize)
    .Select(x => new AgendamentoAdminResponse
    {
        Id = x.Id,
        FornecedorNome = x.Fornecedor!.Nome,
        MotoristaNome = x.Usuario!.Motorista!.NomeReal,
        PlacaVeiculo = x.PlacaVeiculo ?? x.NotaFiscal!.PlacaVeiculo,
        LocalDescarga = x.LocalDescarga!.Nome,
        DataInicio = x.DataInicio,
        Status = x.StatusAgendamento.ToString(),
    })
    .ToListAsync(ct);
```

Dentro de expressão projetada, `x.Fornecedor!.Nome` vira `LEFT JOIN` e devolve `null` sem exceção — o `!` só cala o compilador. Não chame métodos C# arbitrários dentro do `Select` (avaliação no cliente ou erro de tradução).

Trade-off conhecido: a projeção no repositório faz o repositório depender do DTO do Domain. Aceito no projeto por performance (`AgendamentoRepositorio.GetAdminViewAsync`). Alternativa mais limpa quando o mapeamento crescer: o repositório expõe `IQueryable<T>` filtrado e o service projeta — decida por entidade, não misture no mesmo agregado.

**No TruckFlow:** `AgendamentoRepositorio.GetAdminViewAsync` projeta; `GradeRepositorio.GetPagedAsync` materializa entidade e mapeia depois (ok para volume pequeno).

Referência: [EF Core — Efficient querying (project only properties you need)](https://learn.microsoft.com/ef/core/performance/efficient-querying#project-only-properties-you-need) · [Jon P Smith — EF Core in Action, cap. "Performance tuning"](https://www.thereformedprogrammer.net/)

### 4.4 Log estruturado com template fixo, contexto de tenant e zero PII/segredo

**Impacto:** MEDIUM-HIGH — logs consultáveis por EmpresaId no Loki/Seq e sem achado LGPD no pen-test  
**Arquivo:** `rules/service-structured-logging.md`

Injete `ILogger<T>` (abstração Microsoft; Serilog é o provider). Use **message template** com placeholders PascalCase — nunca interpolação `$"..."`, que destrói a estrutura e impede busca por propriedade. Inclua `EmpresaId` e o ID de domínio em eventos relevantes: com 21 fábricas, "qual fábrica?" é sempre a primeira pergunta do incidente (ADR-0004 itens 9-10).

Níveis: `Information` = fato de negócio (agendamento criado, check-in); `Warning` = falha esperada/recuperável (validação, concorrência, SEFAZ fora); `Error` = falha inesperada com exceção. Request/duração já são logados por `RequestLoggingMiddleware` e `ActionLoggingFilter` — não duplique.

**Nunca logue:** senha, token/refresh token, `Authorization`, certificado/senha do A1, CPF completo, telefone, e-mail completo, localização precisa do motorista. Chave de acesso NF-e: só os últimos dígitos se necessário.

**Incorreto:**

```csharp
_logger.LogInformation($"Agendamento {vaga.Id} reservado por {motorista.Cpf} tel {motorista.Telefone}");
_logger.LogInformation("Login: {@Dto}", loginDto);    // destructuring loga a senha
```

**Correto:**

```csharp
_logger.LogInformation(
    "Agendamento avulso criado: {AgendamentoId} (empresa {EmpresaId}, status {Status})",
    vaga.Id, empresaId, vaga.StatusAgendamento);

_logger.LogWarning(ex, "Concorrência ao reservar {AgendamentoId} pelo motorista {MotoristaId}",
    vaga.Id, motoristaId);
```

Para hot paths, `LoggerMessage` source generator (`[LoggerMessage(...)]`) evita alocação. Ao implementar o `CorrelationIdMiddleware` (hoje vazio), use `ILogger.BeginScope` / `LogContext.PushProperty` com `TraceId`, `EmpresaId`, `UserId`.

**No TruckFlow:** templates em `AgendamentoAdminService` e workers seguem o padrão. Config Serilog em `TruckFlow/Configuration/SerilogConfiguration.cs` (sem enrichers ainda; Console listado 2x no appsettings).

Referência: [Serilog — Writing log events / message templates](https://github.com/serilog/serilog/wiki/Writing-Log-Events) · [Nicholas Blumhardt — structured logging](https://nblumhardt.com/) · [High-performance logging (LoggerMessage)](https://learn.microsoft.com/dotnet/core/extensions/high-performance-logging) · [LGPD — Lei 13.709/2018, art. 6º (minimização)](https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm)

### 4.5 Transação explícita só quando há mais de um SaveChanges — curta e com await using

**Impacto:** HIGH — reserva + evento de recebimento comitam juntos ou nenhum comita  
**Arquivo:** `rules/service-transactions.md`

Um único `SaveChangesAsync` já é atômico. Como os repositórios do TruckFlow chamam `SaveChangesAsync` por conta própria, **dois** métodos de repositório no mesmo caso de uso são dois commits — se o segundo falhar, o primeiro fica. Nesse caso abra transação via `_repo.BeginTransactionAsync(ct)` com `await using` (rollback automático no dispose se não houver commit).

Regras:
- Nada de I/O externo dentro da transação (SEFAZ, Expo, e-mail, geocoding) — segura lock e conexão. Efeito externo vai para o **outbox** (evento de domínio) e roda depois do commit.
- Transação curta: carregue o que precisa antes, abra, escreva, comite.
- Os `catch` de tradução de erro Postgres ficam **em volta** do commit (ver `error-map-postgres-sqlstate`).

**Incorreto:**

```csharp
await _repo.Update(vaga, ct);                         // commit 1
await _lifecycle.AoReservarAsync(vaga, peso, ct);     // commit 2 — se falhar, vaga reservada sem evento
await _expo.SendAsync(push, ct);                      // HTTP externo no meio do fluxo
```

**Correto:**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
try
{
    await _repo.Update(vaga, ct);
    await _lifecycle.AoReservarAsync(vaga, pesoEstimado, ct);
    await tx.CommitAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
// push: vaga.Reservar(...) adicionou evento → OutboxSaveChangesInterceptor → PushDispatcherWorker
```

Evolução de design (registrar em ADR se for adotar): repositórios que **só fazem staging** e um `IUnitOfWork.SaveChangesAsync` no service eliminam a necessidade de transação explícita na maioria dos casos (Fowler, PoEAA — Unit of Work). Hoje o projeto mistura os dois estilos; em agregado novo, escolha um e seja consistente nele.

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment`, `AgendamentoAdminService` (Create/Update/Cancelar/Finalizar).

Referência: [EF Core — Transactions](https://learn.microsoft.com/ef/core/saving/transactions) · [Martin Fowler — Unit of Work](https://martinfowler.com/eaaCatalog/unitOfWork.html) · [Milan Jovanović — Outbox pattern](https://www.milanjovanovic.tech/blog/outbox-pattern-for-reliable-microservices-messaging)

### 4.6 Validator e regra de tempo usam TimeProvider em UTC, avaliado na hora

**Impacto:** HIGH — "data já passou" deixa de depender do fuso do servidor e da hora em que o validator foi construído  
**Arquivo:** `rules/service-validator-clock.md`

`GreaterThan(DateTime.Now.AddMinutes(-5))` tem dois bugs: (1) `DateTime.Now` é hora **local do servidor** (Railway/Azure = UTC, máquina do dev = BRT) enquanto o domínio grava UTC; (2) o argumento é avaliado **uma vez**, quando o validator é construído — com lifetime Singleton ou cache, a "agora" congela. Use `TimeProvider` (.NET 8), avaliado dentro de `Must`, e compare UTC com UTC. Bônus: em teste você injeta `FakeTimeProvider` e testa a virada de janela sem `Thread.Sleep`.

**Incorreto:**

```csharp
RuleFor(x => x.DataInicio)
    .GreaterThan(DateTime.Now.AddMinutes(-5))     // local + congelado na construção
    .WithMessage("A data informada já passou.");
```

**Correto:**

```csharp
public sealed class AgendamentoAdminCreateValidator : AbstractValidator<AgendamentoAdminCreateDto>
{
    public AgendamentoAdminCreateValidator(TimeProvider clock)
    {
        RuleFor(x => x.DataInicio)
            .Must(d => d.ToUniversalTime() > clock.GetUtcNow().UtcDateTime.AddMinutes(-5))
            .WithMessage("A data informada já passou.");
    }
}

// Program.cs
builder.Services.AddSingleton(TimeProvider.System);
```

Regra geral de tempo: persistir e comparar em **UTC**; converter para `Grade.OperationalTimeZone` (America/Sao_Paulo) só na borda (montar slots do dia, exibir). Nunca `DateTime.Now` em código de servidor.

**No TruckFlow:** dívida em `Validators/AgendamentoAdmin/AgendamentoAdminCreateValidator.cs`. Entidades e services hoje usam `DateTime.UtcNow` direto — ok; ao tocar numa regra temporal testável, migre aquele ponto para `TimeProvider`.

Referência: [.NET 8 — TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) · [Microsoft.Extensions.TimeProvider.Testing — FakeTimeProvider](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing) · [Npgsql — Date and time handling (timestamptz = UTC)](https://www.npgsql.org/doc/types/datetime.html)


---

## 5. Modelo de domínio

**Impacto:** HIGH  
**Descrição:** Invariantes e transições de estado dentro das entidades, eventos de domínio via outbox, factories, enums persistidos e tempo em UTC.

### 5.1 Enum persistido tem valor int explícito e nunca é reordenado

**Impacto:** HIGH — evita corromper status de milhares de agendamentos com um "reorganizar enum"  
**Arquivo:** `rules/domain-enums-persisted.md`

Enums do TruckFlow são gravados como `int` e aparecem em SQL cru (a exclusion constraint filtra `"StatusAgendamento" IN (0, 1, 2, 3)`), em payloads do mobile (`PlataformaDispositivo { Ios = 1, Android = 2 }`) e em eventos do outbox já gravados. Reordenar ou inserir no meio muda o significado de dados existentes **sem erro nenhum**.

Regras:
- Todo membro com valor explícito.
- Novo valor sempre no **fim**, com número novo. Nunca reutilizar número removido.
- Remover membro = marcar `[Obsolete]` e manter o número.
- Se o valor aparece em SQL (constraint, índice parcial, view), a migration que adiciona o membro revisa esse SQL.
- Para o front, o DTO expõe `.ToString()` (nome) quando a tela exibe; enum numérico só onde o cliente espelha o enum.

**Incorreto:**

```csharp
public enum StatusAgendamento
{
    Disponivel, Pendente, Agendado, Confirmado /* inserido no meio */, EmAndamento, Finalizado, Cancelado, Expirado
}
```

**Correto:**

```csharp
public enum StatusAgendamento
{
    Disponivel = 0,
    Pendente = 1,
    Agendado = 2,
    EmAndamento = 3,
    Finalizado = 4,
    Cancelado = 5,
    Expirado = 6,
    NoShow = 7,        // novo: no fim — e revisar agendamento_no_overlap_per_doca
}
```

**No TruckFlow:** `TruckFlow.Domain/Enums/*` já usa valores explícitos — mantenha. Front (`src/enums/StatusAgendamento`) e mobile (`src/enums/AgendamentoStatus`) espelham: atualizar os três na mesma feature.

Referência: [EF Core — Value conversions (enums)](https://learn.microsoft.com/ef/core/modeling/value-conversions) · [.NET — Enum design guidelines](https://learn.microsoft.com/dotnet/standard/design-guidelines/enum)

### 5.2 Efeito colateral vira evento de domínio → outbox, nunca chamada inline

**Impacto:** HIGH — notificação/push nunca "some" se o commit falhar e nunca sai se o commit não aconteceu  
**Arquivo:** `rules/domain-events-outbox.md`

Chamar `INotificacaoService`/Expo/e-mail no meio do caso de uso cria o problema do *dual write*: o push sai e a transação faz rollback (motorista notificado de algo que não aconteceu), ou o commit sai e o push falha (ninguém fica sabendo). O TruckFlow já resolve com **transactional outbox**: a entidade chama `AddDomainEvent(...)`, o `OutboxSaveChangesInterceptor` grava `OutboxEvent` **no mesmo SaveChanges**, e o `OutboxProcessorWorker` (claim com `FOR UPDATE SKIP LOCKED`, retry exponencial até 8 tentativas) despacha para `IDomainEventHandler<T>` sob `WithTenant(EmpresaId)`.

**Incorreto:**

```csharp
agendamento.AlterarStatus(StatusAgendamento.Cancelado);
await _repo.Update(agendamento, ct);
await _notificacaoService.EnviarPushAsync(agendamento.UsuarioId, "Agendamento cancelado", ct); // dual write
```

**Correto:**

```csharp
// Domain/Events — record imutável com EmpresaId, OcorridoEm e IdempotencyKey
public sealed record AgendamentoCanceladoEvent(
    Guid AgendamentoId, Guid EmpresaId, Guid? MotoristaUsuarioId,
    string? MotivoCancelamento, DateTime OcorridoEm) : IDomainEvent { /* IdempotencyKey */ }

// Entidade
public void Cancelar(string? motivo = null)
{
    AlterarStatus(StatusAgendamento.Cancelado);
    AddDomainEvent(new AgendamentoCanceladoEvent(Id, EmpresaId, UsuarioId, motivo, DateTime.UtcNow));
}

// Application/Notificacoes/Handlers — idempotente
public sealed class AgendamentoCanceladoHandler(...) : IDomainEventHandler<AgendamentoCanceladoEvent>
{
    public async Task HandleAsync(AgendamentoCanceladoEvent evt, CancellationToken ct) { /* cria Notificacao + NotificacaoEntrega */ }
}
```

Handlers devem ser **idempotentes** (o outbox entrega *at-least-once*): use `IdempotencyKey` do evento ou unique constraint no efeito. Evento novo = registrar o tipo no `OutboxEventTypeResolver` + handler Scoped na extension `Notificacao`.

**No TruckFlow:** `EntidadeBase.DomainEvents`, `Infra/Database/Interceptors/OutboxSaveChangesInterceptor.cs`, `OutboxProcessorWorker`, `Docs/notificacoes/sse-implementacao.md`, ADR-0002.

Referência: [Chris Richardson — Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html) · [Milan Jovanović — Outbox pattern](https://www.milanjovanovic.tech/blog/outbox-pattern-for-reliable-microservices-messaging) · [Gregor Hohpe — Idempotent Receiver](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html) · [Microsoft — Domain events: design and implementation](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/domain-events-design-implementation)

### 5.3 Factory monta a entidade nova completa — estática e pura quando possível

**Impacto:** MEDIUM — construção num lugar só, testável sem banco, sem entidade "meio preenchida"  
**Arquivo:** `rules/domain-factories.md`

O service resolve dependências (tenant, entidades relacionadas já carregadas e validadas) e entrega tudo para a factory, que só **monta**. Factory estática pura (`GradeFactory.Create(dto, empresaId, produto, ...)`) é a preferida: testa em teste de domínio, sem DI. Factory injetável (`ProdutoFactory(ILocalDescargaRepositorio)`) só se justifica quando a montagem inevitavelmente precisa consultar algo — e mesmo assim, prefira o service carregar e passar.

Responsabilidades da factory: preencher `EmpresaId`, `CreatedAt = DateTime.UtcNow` (não existe default no banco nem interceptor para `CreatedAt`), FKs **e** navegações coerentes, status inicial. Se há invariante de criação, ela lança `DomainException`.

**Incorreto:**

```csharp
// service montando na mão, esquecendo CreatedAt
var vaga = new Agendamento { TipoCarga = dto.TipoCarga, LocalDescargaId = dto.LocalDescargaId };
await _repo.AddAgendamento(vaga, ct);      // CreatedAt = 0001-01-01
```

**Correto:**

```csharp
public static class TransportadoraFactory
{
    public static Transportadora Create(TransportadoraCreateDto dto, Guid empresaId) => new()
    {
        RazaoSocial = dto.RazaoSocial.Trim(),
        Cnpj = dto.Cnpj,
        EmpresaId = empresaId,
        CreatedAt = DateTime.UtcNow,
    };
}

// service
var empresaId = _currentUser.GetEmpresaId();
var entidade = TransportadoraFactory.Create(dto, empresaId);
```

Factory estática **não** precisa de `AddTransient<XFactory>()`.

**No TruckFlow:** `Application/Factories/GradeFactory.cs` (referência). Dívidas: `AgendamentoFactory.cs` inteiro comentado; `AgendamentoAdminService.CreateAvulso` não seta `CreatedAt`. Caminho de evolução: mover a montagem para um `static Create(...)` na própria entidade (Evans/Vernon — factory method no agregado) quando o agregado ganhar setters privados.

Referência: Eric Evans, *DDD* — Factories · Vaughn Vernon, *Implementing Domain-Driven Design* — cap. 11 · [Steve Smith — Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses)

### 5.4 Transição de estado mora na entidade, validada por regra central

**Impacto:** HIGH — nenhum caminho de código consegue levar um agendamento Finalizado de volta a Disponivel  
**Arquivo:** `rules/domain-state-transitions.md`

O modelo do TruckFlow é híbrido (setters públicos por causa do EF, mas comportamento nas entidades). A regra que **não** é negociável: status muda só por método de domínio (`AlterarStatus`, `Reservar`, `Cancelar`, `Reagendar`, `Expirar`), que consulta a tabela de transições em `Domain/Rules/StatusAgendamentoRules.cs` (`PodeTransitarPara`) e lança `DomainException` se inválida. Service que faz `agendamento.StatusAgendamento = ...` pula a invariante e o evento de domínio.

**Incorreto (Transaction Script atropelando o domínio):**

```csharp
agendamento.StatusAgendamento = StatusAgendamento.Cancelado;   // sem validação, sem evento
agendamento.UpdatedAt = DateTime.UtcNow;
await _repo.Update(agendamento, ct);
```

**Correto:**

```csharp
// Domain/Entities/Agendamento.cs
public void AlterarStatus(StatusAgendamento novoStatus)
{
    if (!StatusAgendamento.PodeTransitarPara(novoStatus))
        throw new DomainException($"Transição inválida: {StatusAgendamento} → {novoStatus}");
    StatusAgendamento = novoStatus;
    UpdatedAt = DateTime.UtcNow;
}

public void Cancelar(string? motivo = null)
{
    AlterarStatus(StatusAgendamento.Cancelado);
    AddDomainEvent(new AgendamentoCanceladoEvent(
        AgendamentoId: Id, EmpresaId: EmpresaId, MotoristaUsuarioId: UsuarioId,
        MotivoCancelamento: motivo, OcorridoEm: DateTime.UtcNow));
}

// Service
agendamento.Cancelar(dto.Motivo);
await _repo.Update(agendamento, ct);
```

Nova transição = editar **só** `StatusAgendamentoRules` + teste de domínio cobrindo a matriz Allowed/Denied (ADR-0004 item 14). Mensagem de `DomainException` chega ao usuário — escreva legível.

Caminho de evolução (sem quebrar EF): `private set` em `StatusAgendamento` (EF Core materializa por backing field/setter privado) para o compilador impedir atribuição direta. Faça por entidade, com a suíte verde.

**No TruckFlow:** `Agendamento.AlterarStatus/Cancelar/Reservar/Expirar`, `Grade.GerarSlots()`, `RecebimentoEvento.CriarOrfao(...)`.

Referência: [Martin Fowler — Anemic Domain Model](https://martinfowler.com/bliki/AnemicDomainModel.html) · [Vladimir Khorikov — Always-valid domain model](https://enterprisecraftsmanship.com/posts/always-valid-domain-model/) · Eric Evans, *Domain-Driven Design* (2003) · [EF Core — Backing fields](https://learn.microsoft.com/ef/core/modeling/backing-field)

### 5.5 UTC em tudo; America/Sao_Paulo só na borda via Grade.OperationalTimeZone

**Impacto:** HIGH — janelas de descarga não "andam" 3h entre dev, Railway e Azure  
**Arquivo:** `rules/domain-utc-timezone.md`

Npgsql 6+ mapeia `DateTime` com `Kind=Utc` para `timestamptz` e **rejeita** `Kind=Local/Unspecified` nesse tipo — o que é bom: força a disciplina. Toda data gravada e comparada é UTC (`DateTime.UtcNow`). Conversão para o horário operacional da fábrica (`Grade.OperationalTimeZone` = America/Sao_Paulo) acontece apenas onde a regra é "de calendário": gerar slots do dia, "agendamentos de hoje", relatório diário, exibição. Front e mobile recebem ISO-8601 com `Z` e formatam localmente.

**Incorreto:**

```csharp
var hoje = DateTime.Today;                                   // fuso do servidor
var doDia = _db.Agendamento.Where(a => a.DataInicio.Date == hoje);  // "dia" em UTC ≠ dia na fábrica
```

**Correto:**

```csharp
var tz = Grade.OperationalTimeZone;
var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
var inicioLocal = agoraLocal.Date;
var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
var fimUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddDays(1), tz);

var doDia = _db.Agendamento.Where(a => a.DataInicio >= inicioUtc && a.DataInicio < fimUtc); // sargável
```

Intervalos sempre semiabertos `[início, fim)` — igual à exclusion constraint (`tstzrange(..., '[)')`). `DateOnly` em `XListQueryDto` (filtro de tela) é dia **operacional**: converta para o intervalo UTC no service. Se um dia houver fábrica fora de BRT, o fuso vira configuração da `Empresa` (`Empresa.Configuracoes`, ADR-0001) — não espalhe `"America/Sao_Paulo"` literal.

**No TruckFlow:** `Grade.OperationalTimeZone`; dívida de `DateTime.Now` em `AgendamentoAdminCreateValidator` (ver `service-validator-clock`).

Referência: [Npgsql — Date and Time Handling](https://www.npgsql.org/doc/types/datetime.html) · [PostgreSQL — Don't use timestamp without time zone](https://wiki.postgresql.org/wiki/Don%27t_Do_This#Don.27t_use_timestamp_.28without_time_zone.29) · [Jon Skeet — NodaTime / date-time pitfalls](https://nodatime.org/)


---

## 6. Acesso a dados com EF Core

**Impacto:** HIGH  
**Descrição:** Repositórios por agregado, nulabilidade honesta, leitura sem tracking, async de ponta a ponta e concorrência otimista com `xmin`. Para SQL/índices/locks profundos, ver skill `postgres-best-practices`.

### 6.1 Async de ponta a ponta — nunca .Result, .Wait() ou GetAwaiter().GetResult()

**Impacto:** HIGH — evita thread pool starvation sob carga (21 fábricas, pico de reservas)  
**Arquivo:** `rules/data-async-all-the-way.md`

Bloquear em código assíncrono segura uma thread do pool enquanto o I/O acontece. Sob carga, o pool esgota, requests enfileiram e a API "trava" sem CPU alta — o sintoma clássico de *thread pool starvation*. Toda chamada que toca banco, HTTP ou disco é `async`/`await` com o `CancellationToken` propagado. Sem `async void` (exceto event handler), sem `Task.Run` para "tornar async" código que já é async.

**Incorreto:**

```csharp
// Program.cs
scope.ServiceProvider.SeedRolesAsync().GetAwaiter().GetResult();

var empresa = _repo.GetById(id, ct).Result;
public async void Processar() { ... }
```

**Correto:**

```csharp
// Program.cs — top-level statements aceitam await
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.SeedRolesAsync();
}

var empresa = await _repo.GetById(id, ct);
public async Task ProcessarAsync(CancellationToken ct) { ... }
```

Exceção legítima e única: biblioteca **só** síncrona (Zeus NFe) — ver `async-sync-library-wrap`. `ValueTask` só onde a medição justificar.

**No TruckFlow:** dívida em `Program.cs:165` (`SeedRolesAsync().GetAwaiter().GetResult()`), que vai sair junto com as migrations do startup (ADR-0004 item 2).

Referência: [David Fowler — Async Guidance](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md) · [Stephen Cleary — Async/Await Best Practices](https://learn.microsoft.com/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming) · [Diagnosing thread pool starvation](https://learn.microsoft.com/dotnet/core/diagnostics/debug-threadpool-starvation)

### 6.2 Assinatura honesta — FirstOrDefault retorna Task<T?>, sem "!" para calar o compilador

**Impacto:** HIGH — NullReferenceException 500 vira NotFoundException 404 com mensagem  
**Arquivo:** `rules/data-honest-nullability.md`

Os projetos têm `<Nullable>enable</Nullable>`. O valor disso é o compilador obrigar o service a tratar o "não achou". `return agendamento!;` numa assinatura `Task<Agendamento>` mente para o compilador: o service não faz `?? throw`, o null escapa, e o erro aparece 3 linhas depois como `NullReferenceException` → 500.

**Incorreto:**

```csharp
public async Task<Agendamento> GetById(Guid id, CancellationToken token = default)
{
    var agendamento = await _db.Agendamento.Include(...).FirstOrDefaultAsync(x => x.Id == id, token);
    return agendamento!;          // pode ser null
}
```

**Correto:**

```csharp
public Task<Agendamento?> GetById(Guid id, CancellationToken ct) =>
    _db.Agendamento
        .Include(x => x.Fornecedor)
        .Include(x => x.LocalDescarga)
        .FirstOrDefaultAsync(x => x.Id == id, ct);

// service
var agendamento = await _repo.GetById(id, ct)
    ?? throw new NotFoundException("Agendamento não encontrado.");
```

`!` só é aceitável em expressão projetada (`x.Fornecedor!.Nome` dentro de `Select`, que vira LEFT JOIN) e em navegação garantida por `Include` + FK obrigatória — com o motivo óbvio no contexto. Ao corrigir a assinatura de um método existente, ajuste os chamadores (o compilador lista) — mudança pequena e segura.

**No TruckFlow:** dívida em `IAgendamentoRepositorio.GetById` / `AgendamentoRepositorio.GetById`.

Referência: [Nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references) · [EF Core — Working with nullable reference types](https://learn.microsoft.com/ef/core/miscellaneous/nullable-reference-types)

### 6.3 Leitura que não vai ser alterada usa AsNoTracking (ou projeção)

**Impacto:** MEDIUM — menos memória e CPU por request em listagens e dashboards  
**Arquivo:** `rules/data-no-tracking-reads.md`

Change tracking custa snapshot de cada entidade carregada. Em endpoint de leitura (listagem, dashboard, relatório, `GetById` só para exibir), use `.AsNoTracking()` — ou projete com `Select`, que já não rastreia. **Não** use `AsNoTracking` em query cuja entidade vai receber método de domínio e `SaveChanges` (a alteração não seria detectada; e com `xmin` você perde a checagem de concorrência).

**Incorreto:**

```csharp
public Task<List<Produto>> ListarParaDropdown(CancellationToken ct) =>
    _db.Produto.Include(p => p.LocalDescarga).ToListAsync(ct);   // rastreia tudo para só exibir
```

**Correto:**

```csharp
public Task<List<ProdutoOpcaoResponse>> ListarParaDropdown(CancellationToken ct) =>
    _db.Produto
        .OrderBy(p => p.Nome)
        .Select(p => new ProdutoOpcaoResponse { Id = p.Id, Nome = p.Nome })
        .ToListAsync(ct);

public Task<Grade?> GetParaExibir(Guid id, CancellationToken ct) =>
    _db.Grade.AsNoTracking().Include(g => g.Produto).FirstOrDefaultAsync(g => g.Id == id, ct);
```

O global query filter de tenant continua valendo com `AsNoTracking`. Se o mesmo método de repositório serve leitura e escrita, separe em dois (`GetById` rastreado para escrita, `GetParaExibir`/projeção para leitura) em vez de um parâmetro `bool tracking`.

**No TruckFlow:** `DashboardRepositorio` e listagens são candidatos naturais. Para N+1 e Include vs split query, ver `postgres-best-practices/data-n-plus-one`.

Referência: [EF Core — Tracking vs. No-Tracking Queries](https://learn.microsoft.com/ef/core/querying/tracking) · [EF Core — Performance: efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying)

### 6.4 Um repositório sealed por agregado, sem repositório genérico

**Impacto:** MEDIUM-HIGH — queries com nome de negócio, Include certo por caso de uso, nada de IQueryable vazando  
**Arquivo:** `rules/data-repository-per-aggregate.md`

O `DbContext` já é Unit of Work + Repository genérico (Microsoft Learn e Jon P Smith). Um `IRepository<T>` com `GetAll/Add/Update` por cima só adiciona indireção e esconde as queries que importam. O padrão da casa: `XRepositorio` **sealed**, primary constructor `(AppDbContext db)`, interface `IXRepositorio` em `Infra/Repositories/Interfaces/`, métodos com **nome de caso de uso** (`GetConflitosAsync`, `GetAdminViewAsync`, `ObterPorChaveAcrossTenantsAsync`) e os `Include` que aquele caso precisa.

**Incorreto:**

```csharp
public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => db.Set<T>();     // regra de query espalhada pelos services
    public Task<List<T>> GetAll() => db.Set<T>().ToListAsync();
}
```

**Correto:**

```csharp
public sealed class TransportadoraRepositorio(AppDbContext db) : ITransportadoraRepositorio
{
    private readonly AppDbContext _db = db;

    public Task<Transportadora?> GetById(Guid id, CancellationToken ct) =>
        _db.Transportadora.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken ct) =>
        _db.Transportadora.AnyAsync(x => x.Cnpj == cnpj, ct);     // filtro de tenant aplicado pelo global filter

    public async Task<Transportadora> Add(Transportadora entidade, CancellationToken ct)
    {
        await _db.Transportadora.AddAsync(entidade, ct);
        await _db.SaveChangesAsync(ct);
        return entidade;
    }
}
```

Um agregado, um repositório: `NotaFiscalItem` é acessado via `NotaFiscal`, não tem repositório de escrita próprio (exceto leitura especializada justificada, como em Conferência). Specification pattern (Ardalis.Specification) é opção se as queries de um agregado explodirem em combinações — decidir por ADR.

**No TruckFlow:** `Infra/Repositories/AgendamentoRepositorio.cs`. Dívida arquitetural conhecida: interfaces de repositório no Infra e Application referenciando Infra (inversão de dependência incompleta) — não corrija de passagem; é refactor com ADR.

Referência: [Microsoft — Design the infrastructure persistence layer](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) · [Jon P Smith — Is the repository pattern useful with EF Core?](https://www.thereformedprogrammer.net/is-the-repository-pattern-useful-with-entity-framework-core/) · [Steve Smith — Ardalis.Specification](https://github.com/ardalis/Specification) · [Martin Fowler — Repository](https://martinfowler.com/eaaCatalog/repository.html)

### 6.5 Recurso disputado usa xmin como token de concorrência otimista

**Impacto:** HIGH — 100 motoristas na mesma vaga = exatamente 1 reserva (provado em teste)  
**Arquivo:** `rules/data-xmin-concurrency.md`

Qualquer entidade que dois atores podem alterar ao mesmo tempo com regra "o primeiro ganha" (vaga de agendamento, item de planejamento, notificação, entrega, outbox, dispositivo) usa a coluna de sistema `xmin` do Postgres como concurrency token — sem coluna extra, sem trigger. O EF inclui `WHERE xmin = @original` no UPDATE; zero linhas afetadas → `DbUpdateConcurrencyException`, que o service traduz (ver `error-map-postgres-sqlstate`).

**Incorreto (last write wins):**

```csharp
var vaga = await _repo.GetById(id, ct);
if (vaga!.StatusAgendamento != StatusAgendamento.Disponivel) throw new BusinessException("Indisponível");
vaga.Reservar(motoristaId);          // entre o if e o UPDATE, outro motorista reservou
await _repo.Update(vaga, ct);        // sobrescreve sem erro
```

**Correto:**

```csharp
// Infra/Database/EntitiesMapping/TransportadoraConfiguracao.cs
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

```csharp
try
{
    vaga.Reservar(motoristaId);
    await _repo.Update(vaga, ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

Combine camadas: `xmin` (otimista), exclusion/unique constraint (invariante no banco, ver `postgres-best-practices`) e checagem prévia só para mensagem amigável. Para fila de trabalho entre instâncias, otimista não serve — use `FOR UPDATE SKIP LOCKED` (ver `async-skip-locked-claim`).

**No TruckFlow:** `AgendamentoConfiguracao`, migration `20260510025303_concurrency`; prova em `TruckFlow.Test/Integration/AgendamentoReservaConcorrenciaTests.cs` (100 threads → 1 sucesso, 99 falhas).

Referência: [Npgsql EF — Concurrency tokens (xmin)](https://www.npgsql.org/efcore/modeling/concurrency.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)


---

## 7. Injeção de dependência e configuração

**Impacto:** MEDIUM  
**Descrição:** Uma extension por feature, lifetimes coerentes, HttpClient tipado com resiliência e Options validadas no startup.

### 7.1 Uma extension AddX() por feature, chamada no Program.cs, sem registro duplicado

**Impacto:** MEDIUM — feature nova não "funciona no meu teste e quebra em runtime" por DI faltando  
**Arquivo:** `rules/di-feature-extensions.md`

Convenção: `TruckFlow/Extensions/<Feature>/<Feature>DependencyInjection.cs` com `public static IServiceCollection Add<Feature>(this IServiceCollection services)`, registrando service, repositório, factory injetável e validators da feature, e chamada uma vez no `Program.cs`. O nome do método **é o nome da feature** — copiar a extension de outra feature e esquecer de renomear o método gera um `AddProduto` duplicado que nunca é chamado.

Repositório compartilhado (ex.: `IProdutoRepositorio` usado por Grade, Empresa, Fornecedor) é registrado **só na feature dona**. Se outra feature precisar garantir presença, use `TryAddScoped` — nunca `AddTransient` de novo.

**Incorreto:**

```csharp
// Extensions/Veiculo/VeiculoDependencyInjection.cs
public static IServiceCollection AddProduto(this IServiceCollection services)   // nome copiado
{
    services.AddTransient<IVeiculoService, VeiculoService>();
    services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();          // duplicado de novo
    return services;
}
// Program.cs nunca chama → VeiculoController falha ao ativar
```

**Correto:**

```csharp
namespace TruckFlow.Extensions.Veiculo;

public static class VeiculoDependencyInjection
{
    public static IServiceCollection AddVeiculo(this IServiceCollection services)
    {
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IVeiculoRepositorio, VeiculoRepositorio>();
        services.AddScoped<IValidator<VeiculoCreateDto>, VeiculoCreateValidator>();
        return services;
    }
}

// Program.cs
builder.Services.AddVeiculo();
```

Rede de segurança barata: `builder.Host.UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; });` em Development — falha no startup se faltar registro ou houver captive dependency.

**No TruckFlow:** bug real em `Extensions/Veiculo/VeiculoDependencyInjection.cs` (`AddProduto`, nunca chamado). Duplicatas: `IProdutoRepositorio` em Grade/Empresa/Fornecedor, `ILocalDescargaRepositorio` em Produto/LocalDescarga, `IFornecedorRepositorio` em Grade/Fornecedor. Typo `AddItemPlanejameto()`.

Referência: [Dependency injection guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines) · [Andrew Lock — ValidateOnBuild / ValidateScopes](https://andrewlock.net/new-in-asp-net-core-3-service-provider-validation/)

### 7.2 HttpClient tipado via IHttpClientFactory com resilience handler padrão

**Impacto:** MEDIUM-HIGH — Expo/Resend/geocoding instáveis não derrubam o worker nem esgotam sockets  
**Arquivo:** `rules/di-httpclient-resilience.md`

`new HttpClient()` por chamada esgota sockets; `HttpClient` estático ignora troca de DNS. O projeto já usa `AddHttpClient<IExpoPushClient, ExpoPushClient>` (typed) e named clients para geocoding — mas **sem nenhuma política de resiliência**, só timeout. Em .NET 8 o caminho oficial é `Microsoft.Extensions.Http.Resilience` (Polly v8): `AddStandardResilienceHandler()` aplica rate limiter, timeout total, retry com backoff exponencial + jitter, circuit breaker e timeout por tentativa.

**Incorreto:**

```csharp
var http = new HttpClient();
var resp = await http.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", msgs, ct);
```

**Correto:**

```csharp
services.AddHttpClient<IExpoPushClient, ExpoPushClient>(c =>
    {
        c.BaseAddress = new Uri("https://exp.host");
    })
    .AddStandardResilienceHandler(o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        o.Retry.MaxRetryAttempts = 3;
    });
```

Cuidados:
- **Retry só em operação idempotente** ou com chave de idempotência. POST de push em lote: o worker já tem retry com backoff via `NotificacaoEntrega` — evite retry duplo (desligue o retry do handler e mantenha circuit breaker/timeout, ou vice-versa, e documente).
- Circuit breaker aberto = falha rápida; o worker loga `Warning` e reagenda.
- SEFAZ via Zeus **não** usa HttpClient (lib própria, síncrona) — resiliência lá é timeout + `Task.Run` com cancelamento + fallback de UX (ver `async-sync-library-wrap`).

**No TruckFlow:** `Extensions/Notificacao` (Expo), `Extensions/Email` (Resend), `Extensions/Geocoding` (Nominatim/Google).

Referência: [Build resilient HTTP apps (Microsoft.Extensions.Http.Resilience)](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) · [IHttpClientFactory guidelines](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines) · [Polly v8](https://www.pollydocs.org/)

### 7.3 Lifetimes coerentes — Scoped para o que toca DbContext, nunca captive dependency

**Impacto:** MEDIUM — elimina DbContext compartilhado entre requests e estado de tenant vazando  
**Arquivo:** `rules/di-lifetimes.md`

`AppDbContext` e `IEmpresaContext` são **Scoped** (um por request). Regras:

| Tipo | Lifetime |
|---|---|
| Services e repositórios (dependem de DbContext) | **Scoped** em código novo |
| Validators FluentValidation | Scoped (ou Singleton se sem dependências) |
| `CurrentUserGuard`, `ICurrentUserService`, `IEmpresaContext` | Scoped |
| Factories estáticas | não registra |
| Stateless puro sem dependência Scoped (`OutboxEventTypeResolver`, `TimeProvider`) | Singleton |
| `BackgroundService` | Singleton (hosted) → cria escopo por iteração |

**Captive dependency**: Singleton que recebe Scoped (direto ou via Transient) "captura" uma instância do primeiro escopo — DbContext compartilhado entre threads e, aqui, **tenant do primeiro request para todo mundo**. É o pior bug de DI possível num sistema multi-tenant.

Hoje a maioria é Transient sobre DbContext Scoped. Funciona (o Transient recebe o DbContext do escopo), mas é inconsistente (Dashboard/Email são Scoped) e cria instâncias à toa. Em código novo use Scoped; não mude registros existentes em massa sem motivo.

**Incorreto:**

```csharp
services.AddSingleton<INotificacaoService, NotificacaoService>();   // NotificacaoService → AppDbContext (Scoped)

public sealed class PushDispatcherWorker(INotificacaoEntregaRepositorio repo) : BackgroundService { } // captive
```

**Correto:**

```csharp
services.AddScoped<INotificacaoService, NotificacaoService>();

public sealed class PushDispatcherWorker(IServiceScopeFactory scopeFactory, ILogger<PushDispatcherWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificacaoEntregaRepositorio>();
        // ...
    }
}
```

`ValidateScopes = true` em Development detecta captive dependency no startup.

**No TruckFlow:** `CurrentUserService` Transient vs `CurrentUserGuard` Scoped (inconsistente, não perigoso). Workers usam `IServiceScopeFactory` — correto.

Referência: [Service lifetimes](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection#service-lifetimes) · [Andrew Lock — Captive dependencies](https://andrewlock.net/the-dangers-and-gotchas-of-using-scoped-services-when-configuring-options-in-asp-net-core/) · [Mark Seemann — Dependency Injection Principles, Practices, and Patterns](https://blog.ploeh.dk/)

### 7.4 Configuração via Options tipadas com ValidateOnStart

**Impacto:** MEDIUM — chave JWT vazia ou certificado A1 ausente derruba o deploy, não o primeiro request em produção  
**Arquivo:** `rules/di-options-validate-on-start.md`

Ler `configuration["Sefaz:Certificado:Senha"]` espalhado pelo código esconde dependência e só falha quando o fluxo roda. Use classe de options + `BindConfiguration` + validação + `ValidateOnStart()`: config inválida faz a aplicação **não subir** — o healthcheck do Railway (`/health/ready`) segura o deploy e a versão anterior continua no ar.

**Incorreto:**

```csharp
var key = builder.Configuration["JwtOptions:SecurityKey"];     // "" em appsettings → assina com chave vazia?
var senha = _config["Sefaz:Certificado:Senha"];                 // descobre no primeiro consumo
```

**Correto:**

```csharp
public sealed class JwtOptions
{
    public const string Section = "JwtOptions";
    [Required, MinLength(64)] public string SecurityKey { get; init; } = "";
    [Required] public string Issuer { get; init; } = "TruckFlow";
    [Required] public string Audience { get; init; } = "TruckFlow";
}

services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddOptions<SefazOptions>()
    .BindConfiguration("Sefaz")
    .Validate(o => o.UseFake || !string.IsNullOrWhiteSpace(o.Certificado.Thumbprint)
                             || !string.IsNullOrWhiteSpace(o.Certificado.Caminho),
              "Sefaz: informe Certificado.Thumbprint ou Certificado.Caminho quando UseFake=false.")
    .ValidateOnStart();
```

Consuma via `IOptions<T>` (config estática) ou `IOptionsMonitor<T>` (recarregável). Segredo nunca tem default real no código nem no `appsettings.json` — vem de user-secrets (dev) e env vars `Secao__Chave` (prod).

**No TruckFlow:** `SefazOptions`, `ResendOptions`, `GeocodingOptions` já existem como classes — falta `ValidateOnStart`. `appsettings` tem `JwtOptions:SecurityKey` vazio esperando env/user-secrets — exatamente o caso que `ValidateOnStart` protege.

Referência: [Options pattern in .NET — validation](https://learn.microsoft.com/dotnet/core/extensions/options#options-validation) · [Andrew Lock — Adding validation to strongly typed configuration](https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-dotnet-6/)


---

## 8. Background services e concorrência entre instâncias

**Impacto:** MEDIUM  
**Descrição:** Workers que funcionam com 2+ instâncias da API (ADR-0004 item 1 e 4), escopo de DI por iteração, cancelamento e bibliotecas síncronas.

### 8.1 BackgroundService com PeriodicTimer, escopo por iteração e falha isolada

**Impacto:** MEDIUM-HIGH — worker não morre na primeira exceção, não segura DbContext eterno e para limpo no deploy  
**Arquivo:** `rules/async-background-service.md`

Anatomia de worker no TruckFlow:
1. Singleton hosted; dependências Scoped via `IServiceScopeFactory.CreateAsyncScope()` **por iteração** (DbContext novo a cada ciclo, change tracker não cresce para sempre).
2. Loop com `PeriodicTimer` (sem drift e sem `Task.Delay` acumulando) respeitando `stoppingToken`.
3. `try/catch` **dentro** do loop: exceção não tratada em `ExecuteAsync` para o host (.NET 8: `BackgroundServiceExceptionBehavior.StopHost` por padrão) — derruba a API inteira.
4. `OperationCanceledException` com `stoppingToken` cancelado = shutdown normal, sai sem logar erro.
5. Trabalho por tenant com `WithTenant` (ver `tenant-workers-with-tenant`) e por lote (ex.: 500).
6. Seguro para **2+ instâncias** (ADR-0004 item 1/4): claim com `FOR UPDATE SKIP LOCKED` ou idempotência.

**Incorreto:**

```csharp
protected override async Task ExecuteAsync(CancellationToken ct)
{
    var db = _sp.GetRequiredService<AppDbContext>();        // Scoped resolvido do root → captive
    while (true)
    {
        await Processar(db);                                 // 1 exceção mata o worker (e o host)
        await Task.Delay(60000);                             // ignora cancelamento
    }
}
```

**Correto:**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
    do
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IAgendamentoExpirationService>();
            var empresaContext = scope.ServiceProvider.GetRequiredService<IEmpresaContext>();
            // foreach empresa → WithTenant → service.ExpirarVencidosAsync(stoppingToken)
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ciclo do {Worker} falhou; tentando no próximo.", nameof(AgendamentoExpirationWorker));
        }
    }
    while (await timer.WaitForNextTickAsync(stoppingToken));
}
```

**No TruckFlow:** `AgendamentoExpirationWorker`, `OutboxProcessorWorker`, `PushDispatcherWorker`, `ReceiptPollerWorker`, `RealtimeNotificationListener` (conexão LISTEN dedicada com reconexão em 5s). ADR-0004 item 4: expiração precisa de claim distribuído antes do multi-instance.

Referência: [Background tasks with hosted services](https://learn.microsoft.com/aspnet/core/fundamentals/host/hosted-services) · [.NET 6+ — unhandled exceptions in BackgroundService](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/6.0/hosting-exception-handling) · [PeriodicTimer](https://learn.microsoft.com/dotnet/api/system.threading.periodictimer)

### 8.2 Fila de trabalho entre instâncias = claim com FOR UPDATE SKIP LOCKED

**Impacto:** HIGH — 2 instâncias nunca processam o mesmo evento/push/expiração  
**Arquivo:** `rules/async-skip-locked-claim.md`

Com 2+ instâncias da API (pré-requisito de rollout, ADR-0004), todo worker roda N vezes em paralelo. `SELECT ... WHERE status = pendente` sem lock faz duas instâncias pegarem o mesmo item (push duplicado, evento processado duas vezes). Lock otimista (`xmin`) só detecta depois. O padrão é **claim** atômico dentro de uma transação curta: `FOR UPDATE SKIP LOCKED` faz cada instância pular as linhas já travadas pela outra.

**Incorreto:**

```csharp
var pendentes = await db.OutboxEvent
    .Where(e => e.ProcessadoEm == null && e.ProximaTentativaEm <= agora)
    .Take(50).ToListAsync(ct);         // as duas instâncias leem os mesmos 50
```

**Correto:**

```csharp
await using var tx = await db.Database.BeginTransactionAsync(ct);
var evento = await db.OutboxEvent
    .FromSql($"""
        SELECT * FROM "OutboxEvent"
        WHERE "ProcessadoEm" IS NULL AND "ProximaTentativaEm" <= {agora}
        ORDER BY "CriadoEm"
        LIMIT 1
        FOR UPDATE SKIP LOCKED
        """)
    .FirstOrDefaultAsync(ct);
if (evento is null) return;
// processa sob WithTenant(evento.EmpresaId), marca processado, commit
await tx.CommitAsync(ct);
```

Regras: transação curta (sem I/O externo longo segurando o lock — para push, marque "em envio" e comite, envie, depois registre o resultado); `ORDER BY` com índice parcial nas pendentes; `FromSql` interpolado (parametrizado, sem SQL injection). Para "só uma instância roda este job" (não fila), `pg_try_advisory_lock` é alternativa.

**No TruckFlow:** `OutboxProcessorWorker` e `NotificacaoEntregaRepositorio` usam; `AgendamentoExpirationService` ainda não (ADR-0004 item 4). Provado em `OutboxDispatcherConcorrenciaTests`.

Referência: [PostgreSQL — SELECT ... FOR UPDATE SKIP LOCKED](https://www.postgresql.org/docs/current/sql-select.html#SQL-FOR-UPDATE-SHARE) · [2ndQuadrant/EDB — What is SKIP LOCKED for in PostgreSQL 9.5?](https://www.enterprisedb.com/blog/what-skip-locked-postgresql-95) · [EF Core — SQL queries (FromSql)](https://learn.microsoft.com/ef/core/querying/sql-queries)

### 8.3 Biblioteca síncrona (Zeus/SEFAZ) — isole, limite o tempo e falhe com UX

**Impacto:** MEDIUM — SEFAZ lenta não segura threads do pool nem deixa o motorista esperando 60s  
**Arquivo:** `rules/async-sync-library-wrap.md`

`Zeus.Net.NFe.NFCe` é síncrona e faz SOAP com certificado A1. `Task.Run(() => ...)` é a única opção para não bloquear o request — mas `Task.Run` **não** cancela trabalho síncrono: o token só evita *iniciar*. Por isso: timeout curto na própria lib, `WaitAsync(timeout, ct)` para devolver a resposta ao usuário a tempo, e fallback claro (o fluxo do mobile cai para upload de XML).

**Incorreto:**

```csharp
public NfeResult Consultar(string chave) => _servicos.NfeConsultaProtocolo(chave);   // bloqueia a thread do request

return await Task.Run(() => ExecutarConsulta(chave, uf, ambiente));   // sem token, sem limite (TimeOut = 60000)
```

**Correto:**

```csharp
public async Task<SefazConsultaResult> ConsultarProtocoloAsync(string chave, string uf, CancellationToken ct)
{
    var ambiente = _options.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;
    try
    {
        return await Task.Run(() => ExecutarConsulta(chave, uf, ambiente), ct)
            .WaitAsync(TimeSpan.FromSeconds(20), ct);
    }
    catch (TimeoutException)
    {
        _logger.LogWarning("SEFAZ não respondeu em 20s para UF {Uf}", uf);
        throw new BusinessException("SEFAZ demorou para responder. Envie o XML da nota ou tente novamente.");
    }
}
```

Mantenha a abstração `ISefazClient` com `FakeSefazClient` (`Sefaz:UseFake`) para dev/teste — integração real só com certificado e ambiente explícitos. Nunca logue senha/serial do certificado.

**No TruckFlow:** `TruckFlow.Application/Sefaz/ZeusSefazClient.cs` (`TimeOut = 60000`, `Task.Run` com token). Contexto de negócio em `Docs/sefaz-certificado-consulta-nfe.md`; agente `sefaz-nfe-specialist`.

Referência: [Stephen Cleary — Task.Run etiquette](https://blog.stephencleary.com/2013/10/taskrun-etiquette-and-proper-usage.html) · [Task.WaitAsync](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.waitasync) · [David Fowler — Async Guidance (avoid sync over async / async over sync)](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)


---

## 9. Testes

**Impacto:** MEDIUM  
**Descrição:** Integração com Postgres real (Testcontainers), sem mock de banco, gate multi-tenant e concorrência obrigatórios.

### 9.1 Recurso disputado tem teste com N tarefas concorrentes, cada uma com seu DbContext

**Impacto:** HIGH — prova "exatamente 1 vencedor" em vez de assumir  
**Arquivo:** `rules/test-concurrency.md`

Concorrência não se prova com teste sequencial. O padrão do projeto: N `Task.Run`, **cada uma** com `EmpresaContext`, `DbContext` e `FakeCurrentUserService` próprios (DbContext não é thread-safe — compartilhar invalida o teste), disparadas juntas, `Task.WhenAll`, e asserts sobre o resultado agregado **e** sobre o estado final no banco (lido com contexto novo).

**Incorreto:**

```csharp
await svc.BookAppointment(dto1);
await Assert.ThrowsAsync<BusinessException>(() => svc.BookAppointment(dto2)); // sequencial: testa o if, não a corrida
```

**Correto (padrão `AgendamentoReservaConcorrenciaTests`):**

```csharp
var tentativas = motoristaIds.Select(motoristaId => Task.Run(async () =>
{
    var empresaContext = ServiceFactory.NewEmpresaContext();
    using var tenantScope = empresaContext.WithTenant(empresaId);
    using var db = Fixture.CreateDbContext(empresaContext);
    var currentUser = new FakeCurrentUserService { UserId = motoristaId, EmpresaId = empresaId, IsMotorista = true };
    var svc = ServiceFactory.CreateAgendamentoMotoristaService(db, empresaContext, currentUser);
    try { await svc.BookAppointment(new ReservarAgendamentoDto { AgendamentoId = vagaId, NotaFiscalChaveAcesso = chave }); return true; }
    catch (Exception ex) when (ex is BusinessException or DomainException or DbUpdateConcurrencyException) { return false; }
})).ToArray();

var resultados = await Task.WhenAll(tentativas);
Assert.Single(resultados, r => r);
Assert.Equal(totalThreads - 1, resultados.Count(r => !r));

await using var assertDb = Fixture.CreateDbContext();
var vagaFinal = await assertDb.Agendamento.IgnoreQueryFilters().SingleAsync(a => a.Id == vagaId);
Assert.Equal(StatusAgendamento.Agendado, vagaFinal.StatusAgendamento);
```

Para workers: dois "workers" consumindo a mesma fila → cada item processado **uma** vez (`OutboxDispatcherConcorrenciaTests`). Capture só as exceções esperadas no `catch` — qualquer outra deve falhar o teste.

**No TruckFlow:** `TruckFlow.Test/Integration/AgendamentoReservaConcorrenciaTests.cs`, `OutboxDispatcherConcorrenciaTests.cs`. Load test de sistema fica em `TruckFlow.LoadTests` (NBomber) — complementar, não substituto.

Referência: [EF Core — DbContext thread safety](https://learn.microsoft.com/ef/core/dbcontext-configuration/#avoiding-dbcontext-threading-issues) · [xUnit — Shared context (collection fixtures)](https://xunit.net/docs/shared-context)

### 9.2 Toda entidade IEmpresaScoped nova ganha teste cross-tenant

**Impacto:** CRITICAL — rede de proteção que impede refactor futuro de reintroduzir vazamento entre fábricas  
**Arquivo:** `rules/test-cross-tenant-mandatory.md`

ADR-0004 item 14: "sem isso o blueprint multi-tenant fica sem rede de proteção — qualquer refactor pode reintroduzir vazamento entre empresas, e isso queima contrato Aurora". O teste mínimo por entidade/feature nova cria dados nas Empresas A e B e prova, **com Postgres real**:

1. Listagem como B não contém nada de A.
2. `GetById` de registro de A como B → `NotFoundException` (não 403).
3. Update/Delete de registro de A como B → `NotFoundException` e o registro de A continua intacto.
4. Contexto **sem tenant** → exceção ou zero linhas, nunca "tudo".

**Incorreto (só testa o caminho feliz de um tenant):**

```csharp
[Fact]
public async Task GetPaged_RetornaTransportadoras()
{
    // seed em uma empresa, lista na mesma empresa, Assert.NotEmpty — não prova isolamento
}
```

**Correto:**

```csharp
[Fact]
public async Task GetById_DeRegistroDeOutraEmpresa_LancaNotFound()
{
    var (empresaA, empresaB) = (await SeedEmpresaAsync("A"), await SeedEmpresaAsync("B"));
    Guid idDeA;

    var ctxA = ServiceFactory.NewEmpresaContext();
    using (ctxA.WithTenant(empresaA))
    await using (var dbA = Fixture.CreateDbContext(ctxA))
    {
        var svcA = ServiceFactory.CreateTransportadoraService(dbA, ctxA, new FakeCurrentUserService { EmpresaId = empresaA });
        idDeA = (await svcA.Create(new() { RazaoSocial = "Da A", Cnpj = "11111111000111" }, default)).Id;
    }

    var ctxB = ServiceFactory.NewEmpresaContext();
    using var scopeB = ctxB.WithTenant(empresaB);
    await using var dbB = Fixture.CreateDbContext(ctxB);
    var svcB = ServiceFactory.CreateTransportadoraService(dbB, ctxB, new FakeCurrentUserService { EmpresaId = empresaB });

    await Assert.ThrowsAsync<NotFoundException>(() => svcB.GetById(idDeA, default));
    var paginaB = await svcB.GetPaged(new() { PageNumber = 1, PageSize = 50 }, default);
    Assert.DoesNotContain(paginaB.Items, x => x.Id == idDeA);
}
```

Para `Usuario` (não escopado pelo filtro), o teste é ainda mais importante — ver `tenant-usuario-manual-check`. Evolução recomendada: uma suíte HTTP com `WebApplicationFactory<Program>` + Testcontainers que bate nos endpoints com JWT de A e B e verifica 404/403 por role — cobre a camada que os testes de service não veem (atributos `[Authorize]`, binding, middleware).

**No TruckFlow:** referência `TruckFlow.Test/Integration/NotificacaoCrossTenantTests.cs`.

Referência: [OWASP API1:2023 — BOLA (how to prevent: tests)](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/) · [Integration tests in ASP.NET Core (WebApplicationFactory)](https://learn.microsoft.com/aspnet/core/test/integration-tests)

### 9.3 Teste de integração com Postgres real (Testcontainers), nunca mock de banco

**Impacto:** HIGH — pega o que só o Postgres faz — xmin, exclusion constraint, filtered unique, SKIP LOCKED, migrations  
**Arquivo:** `rules/test-integration-postgres-real.md`

Regra do projeto (CLAUDE.md, feedback Aurora): **sem mock** em teste de integração — migration de produção já quebrou com mock. InMemory/SQLite não têm `xmin`, `tstzrange`, `EXCLUDE USING gist`, índice filtrado nem `FOR UPDATE SKIP LOCKED`, que são exatamente as garantias críticas do sistema.

Infra pronta em `TruckFlow.Test/Integration/`:
- `DatabaseFixture` (`IAsyncLifetime`): sobe `postgres:16-alpine` via Testcontainers, roda `MigrateAsync()` (migrations reais), `CreateDbContext(empresaContext)` com interceptors de tenant e outbox, `ResetAsync()` com `TRUNCATE ... RESTART IDENTITY CASCADE`.
- `[Collection(IntegrationCollection.Name)]` + `IntegrationTestBase` (reset antes de cada teste).
- `ServiceFactory` monta services com repositórios reais e `NullLogger<T>.Instance`.
- `TestDataSeeder` com builders (`NovaEmpresa`, `NovoProduto`, `NovaVagaDisponivel`).
- Fakes **escritos à mão** para o que não é banco (`FakeCurrentUserService`), sem Moq.

**Incorreto:**

```csharp
var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("x").Options;
var repo = new Mock<IAgendamentoRepositorio>();
repo.Setup(r => r.Update(It.IsAny<Agendamento>(), default)).Returns(Task.CompletedTask); // nunca vai concorrer
```

**Correto:**

```csharp
[Collection(IntegrationCollection.Name)]
public class TransportadoraServiceTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Create_ComCnpjDuplicadoNaMesmaEmpresa_LancaBusinessException()
    {
        var empresaId = await SeedEmpresaAsync();
        var ctx = ServiceFactory.NewEmpresaContext();
        using var scope = ctx.WithTenant(empresaId);
        await using var db = Fixture.CreateDbContext(ctx);
        var svc = ServiceFactory.CreateTransportadoraService(db, ctx,
            new FakeCurrentUserService { EmpresaId = empresaId });

        await svc.Create(new() { RazaoSocial = "A", Cnpj = "12345678000199" }, CancellationToken.None);

        await Assert.ThrowsAsync<BusinessException>(() =>
            svc.Create(new() { RazaoSocial = "B", Cnpj = "12345678000199" }, CancellationToken.None));
    }
}
```

Nomes: `Metodo_Cenario_Resultado` em PT. Asserts com `Assert.*` do xUnit. Teste unitário puro (sem banco) só para domínio/validators/helpers (`TruckFlow.Test/Domain`, `/Application`).

Lacunas conhecidas da fixture: não inclui `AuditSaveChangesInterceptor` nem `RealtimeNotificationInterceptor` — teste que depende de auditoria/NOTIFY precisa adicioná-los. Não há teste HTTP (WebApplicationFactory) — autorização por role não é coberta (ver `test-cross-tenant-mandatory`).

Referência: [Testcontainers for .NET — PostgreSQL](https://dotnet.testcontainers.org/modules/postgres/) · [EF Core — Testing against your production database system](https://learn.microsoft.com/ef/core/testing/testing-with-the-database) · [Vladimir Khorikov — Unit Testing Principles, Practices, and Patterns (cap. 8-10)](https://www.manning.com/books/unit-testing)


---

## 10. Segurança operacional

**Impacto:** MEDIUM-HIGH  
**Descrição:** Segredos fora do repositório, superfície anônima mínima, rate limiting e Swagger fechado em produção — itens que caem direto no questionário SAD da Aurora.

### 10.1 Superfície mínima — revise anônimos, feche Swagger e DevTools em produção

**Impacto:** HIGH — tira do pen-test os achados "fáceis" que derrubam a nota do SAD  
**Arquivo:** `rules/security-attack-surface.md`

Todo endpoint sem `[Authorize]` é público na internet. A lista de anônimos deve ser curta, intencional e revisada a cada PR: login admin/motorista, refresh, registro de motorista, esqueci-senha, callback Entra ID, `/health/*`. Qualquer outro anônimo — especialmente os que **criam** usuário ou empresa — é achado de pen-test (OWASP API5/API3).

**Incorreto:**

```csharp
[HttpPost("register")]                        // anônimo
public async Task<IActionResult> Register([FromBody] RegisterAdminDto dto)
    => Ok(await _usuarioService.RegisterAdminAsync(dto));   // dto.EmpresaId do cliente → admin em qualquer empresa

app.UseSwagger();
app.UseSwaggerUI();                           // em todos os ambientes
```

**Correto:**

```csharp
[HttpPost("register")]
[Authorize(Roles = RoleGroups.CanManageUsers)]           // admin cria admin da PRÓPRIA empresa
public async Task<IActionResult> Register([FromBody] RegisterAdminDto dto, CancellationToken ct)
    => Ok(await _usuarioService.RegisterAdminAsync(dto, ct)); // EmpresaId do guard

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DevToolsController: não registrar em prod (ou [ApiExplorerSettings(IgnoreApi = true)] + feature flag),
// em vez de só um if (IsDevelopment()) dentro da action
```

Checklist por PR: (1) `grep -n "AllowAnonymous\|\[HttpPost" Controllers/` nos arquivos tocados; (2) endpoint novo tem role **e** tenant; (3) nada de rota de debug/seed exposta; (4) CORS com origins explícitas + `AllowCredentials` (cookie de refresh) — sem IP de LAN no fallback de produção.

**No TruckFlow:** `AuthAdminController.Register` (anônimo + `EmpresaId` do body), Swagger UI habilitado em produção, `DevToolsController` (`/v1/dev`, inclusive `seed-entra-login`) protegido só por check em runtime, fallback CORS com `192.168.18.6`.

Referência: [OWASP API Security Top 10 2023](https://owasp.org/API-Security/editions/2023/en/0x11-t10/) · [ASP.NET Core security overview](https://learn.microsoft.com/aspnet/core/security/)

### 10.2 Rate limiting nativo em login, refresh, código de verificação e ingestão de posição

**Impacto:** MEDIUM-HIGH — brute force e cliente com bug não derrubam a API das 21 fábricas  
**Arquivo:** `rules/security-rate-limiting.md`

.NET 8 tem `Microsoft.AspNetCore.RateLimiting` embutido. Políticas mínimas:
- **Login/refresh/enviar-código/verificar-código**: por IP + por login, janela curta (ex.: 5/min) — brute force e enumeração (OWASP API4/API2).
- **`POST /v1/motorista/posicao`**: 1 req/5s por `MotoristaId` (ADR-0003) — app com bug em loop não satura o banco.
- **`/reservar`**: por motorista, protege contra double-submit além da idempotência.

Atrás de proxy (Railway/nginx), a partição por IP depende de `UseForwardedHeaders` **antes** do rate limiter, senão todos os clientes têm o IP do proxy. Com 2+ instâncias, o limiter in-memory é por instância — aceitável como proteção; limite global exige store distribuído (só se medir necessidade).

**Incorreto:**

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] AdminLoginDto dto) => Ok(await _auth.Login(dto)); // ilimitado
```

**Correto:**

```csharp
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy("posicao", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.User.FindFirst("MotoristaId")?.Value ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 1, Window = TimeSpan.FromSeconds(5) }));
});

app.UseForwardedHeaders();
app.UseRouting();
app.UseRateLimiter();          // depois de UseRouting para políticas por endpoint
// ...

[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("login")]
public async Task<IActionResult> Login([FromBody] AdminLoginDto dto, CancellationToken ct) => Ok(await _auth.Login(dto, ct));
```

Resposta 429 deve vir no envelope (`{ success:false, code:"RATE_LIMITED", message:"Muitas tentativas. Aguarde um minuto." }`) via `o.OnRejected`.

Complementos de auth já no projeto/planejados: lockout do Identity, refresh token **rotacionado e armazenado como hash** (`RefreshToken.TokenHash` unique), `ClockSkew = TimeSpan.Zero` no JWT.

**No TruckFlow:** não há rate limiter configurado hoje.

Referência: [Rate limiting middleware in ASP.NET Core](https://learn.microsoft.com/aspnet/core/performance/rate-limit) · [OWASP API4:2023 Unrestricted Resource Consumption](https://owasp.org/API-Security/editions/2023/en/0xa4-unrestricted-resource-consumption/) · [OWASP Authentication Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

### 10.3 Segredo nunca em appsettings/compose versionado — user-secrets em dev, env em prod

**Impacto:** CRITICAL — chave JWT/senha do banco/certificado A1 fora do git; rotação possível  
**Arquivo:** `rules/security-secrets-config.md`

Qualquer valor que dá acesso (JWT signing key, connection string com senha, senha/arquivo do certificado A1, API keys de Resend/Google, credencial Entra ID) fica **fora** do repositório. Dev: `dotnet user-secrets`. Prod: variáveis de ambiente `Secao__Chave` provisionadas pelo provedor (Railway hoje; Key Vault na migração Azure Brasil). `appsettings.json` só carrega chaves com valor vazio/placeholder e `ValidateOnStart` garante que foram preenchidas (ver `di-options-validate-on-start`).

**Incorreto:**

```json
{
  "ConnectionStrings": { "DefaultConnection": "Host=...;Password=SenhaReal123" },
  "JwtOptions": { "SecurityKey": "minha-chave-de-512-bits-..." }
}
```

**Correto:**

```bash
cd src/TruckFlowApi/TruckFlow
dotnet user-secrets set "JwtOptions:SecurityKey" "<gerada com 64+ bytes aleatórios>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=truckflow;Username=...;Password=..."
# prod: JwtOptions__SecurityKey, ConnectionStrings__DefaultConnection, Sefaz__Certificado__Senha
```

Se um segredo já foi commitado, **remover do arquivo não basta**: está no histórico — rotacione (ADR-0004 item 5 registra a rotação pendente da JWT key). Logs nunca imprimem configuração (`IConfiguration` dump) nem connection string.

**No TruckFlow:** `appsettings` já tem `JwtOptions:SecurityKey` vazio. `docker-compose.yml` usa `${POSTGRES_...}` de `.env` (não versionar `.env`). No mobile, `truckflow-driver-firebase-adminsdk-*.json` (service account admin) não deve morar na pasta do app.

Referência: [Safe storage of app secrets in development](https://learn.microsoft.com/aspnet/core/security/app-secrets) · [OWASP Secrets Management Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html) · [Azure Key Vault configuration provider](https://learn.microsoft.com/aspnet/core/security/key-vault-configuration)

