---
title: Usuario não é IEmpresaScoped — cheque EmpresaId na mão
impact: CRITICAL
impactDescription: impede Admin da Empresa B de ler ou inativar usuário da Empresa A
tags: multi-tenant, identity, usermanager, bola
---

## Usuario não é IEmpresaScoped — cheque EmpresaId na mão

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
