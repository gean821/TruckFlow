---
title: EmpresaId (e MotoristaId) vem do JWT, nunca do body
impact: CRITICAL
impactDescription: fecha a classe inteira de bugs "usuário escolhe de qual empresa é o dado"
tags: multi-tenant, security, bola, dto
---

## EmpresaId (e MotoristaId) vem do JWT, nunca do body

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
