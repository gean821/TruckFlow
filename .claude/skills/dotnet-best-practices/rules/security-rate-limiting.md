---
title: Rate limiting nativo em login, refresh, código de verificação e ingestão de posição
impact: MEDIUM-HIGH
impactDescription: brute force e cliente com bug não derrubam a API das 21 fábricas
tags: security, rate-limiting, brute-force, availability
---

## Rate limiting nativo em login, refresh, código de verificação e ingestão de posição

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
