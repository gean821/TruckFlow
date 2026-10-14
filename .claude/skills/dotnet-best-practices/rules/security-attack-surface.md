---
title: Superfície mínima — revise anônimos, feche Swagger e DevTools em produção
impact: HIGH
impactDescription: tira do pen-test os achados "fáceis" que derrubam a nota do SAD
tags: security, authorization, swagger, anonymous, owasp, sad-aurora
---

## Superfície mínima — revise anônimos, feche Swagger e DevTools em produção

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
