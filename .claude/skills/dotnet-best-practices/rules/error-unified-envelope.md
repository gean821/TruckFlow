---
title: Um formato de erro só — unifique o 400 automático do [ApiController]
impact: HIGH
impactDescription: front e mobile param de tratar dois (três) formatos de erro
tags: error-envelope, problem-details, apicontroller, model-binding
---

## Um formato de erro só — unifique o 400 automático do [ApiController]

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
