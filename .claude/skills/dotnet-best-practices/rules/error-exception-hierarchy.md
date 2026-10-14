---
title: Lance a exceção certa da hierarquia — e só ela vira 4xx
impact: CRITICAL
impactDescription: 500 genérico vira 400/404 com mensagem que o operador entende
tags: exceptions, middleware, http-status, error-handling
---

## Lance a exceção certa da hierarquia — e só ela vira 4xx

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
