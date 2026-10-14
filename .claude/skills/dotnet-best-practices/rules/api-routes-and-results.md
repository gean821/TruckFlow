---
title: Rotas v1, status codes corretos e contrato documentado
impact: HIGH
impactDescription: clientes web e mobile recebem semântica HTTP previsível e Swagger fiel
tags: routing, http-status, swagger, openapi, actionresult
---

## Rotas v1, status codes corretos e contrato documentado

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
