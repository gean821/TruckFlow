---
title: Controller magro — recebe, delega ao service, devolve
impact: HIGH
impactDescription: regra de negócio testável sem HTTP e um único lugar para validação/tenant
tags: controller, clean-architecture, separation-of-concerns
---

## Controller magro — recebe, delega ao service, devolve

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
