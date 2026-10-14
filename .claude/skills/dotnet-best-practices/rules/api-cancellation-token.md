---
title: CancellationToken como último parâmetro, propagado até o EF
impact: MEDIUM
impactDescription: request abortado (motorista perdeu sinal, usuário trocou de página) libera conexão do pool
tags: async, cancellation, ef-core, controller
---

## CancellationToken como último parâmetro, propagado até o EF

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
