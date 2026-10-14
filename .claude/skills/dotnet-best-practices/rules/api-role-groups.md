---
title: Autorização com RoleGroups/Roles constantes, nunca string literal
impact: HIGH
impactDescription: renomear/adicionar papel muda um lugar; typo não vira endpoint aberto
tags: authorization, roles, security, controller
---

## Autorização com RoleGroups/Roles constantes, nunca string literal

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
