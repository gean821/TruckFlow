---
title: Toda listagem de tela nasce paginada — XListQueryDto → PagedResponse<T>
impact: HIGH
impactDescription: lista de 21 fábricas não derruba banco nem front; filtros server-side
tags: pagination, query-dto, api-design, performance
---

## Toda listagem de tela nasce paginada — XListQueryDto → PagedResponse<T>

Convenção arquitetural do projeto: nenhuma rota de listagem retorna `List<T>` cru, mesmo que hoje tenha 10 registros. Entrada `[FromQuery] XListQueryDto` com `PageNumber`, `PageSize` e os filtros (`Search`, status, datas, IDs); saída `PagedResponse<TResponse>` (`Items`, `PageNumber`, `PageSize`, `TotalCount`, `TotalPages`). O front sincroniza esses parâmetros com a URL e faz search com debounce **server-side**.

Exceções aceitas: `GetById`, dropdowns curtos/enums, endpoints internos.

**Incorreto:**

```csharp
[HttpGet]
public async Task<IActionResult> GetAll(CancellationToken ct)
    => Ok(await _service.GetAll(ct));     // List<FornecedorResponse> com tudo
```

**Correto:**

```csharp
public class TransportadoraListQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public bool? Ativo { get; set; }
}

[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] TransportadoraListQueryDto query, CancellationToken ct = default)
    => Ok(await _service.GetPaged(query, ct));

// repositório
var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
var pageSize = Math.Clamp(query.PageSize, 1, 100);        // teto evita pageSize=100000
var totalCount = await dbQuery.CountAsync(ct);
var items = await dbQuery
    .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)   // ordenação determinística
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new TransportadoraResponse { Id = x.Id, RazaoSocial = x.RazaoSocial, Cnpj = x.Cnpj })
    .ToListAsync(ct);
```

Sempre **ordenação estável** (desempate por `Id`) — senão itens pulam/repetem entre páginas. Aplique **teto** em `PageSize` (o export CSV do front pede 1000 hoje — export deve ter endpoint próprio). Para tabelas que crescem sem limite (posições, audit), considere keyset pagination (ver `postgres-best-practices/data-pagination`).

**No TruckFlow:** referência `PlanejamentoListQueryDto` + `PlanejamentoRecebimentoService.GetPaged`, `GradeListQueryDto` + `GradeRepositorio.GetPagedAsync`. Fornecedor, Produto, UnidadeEntrega e LocalDescarga ainda retornam lista crua (dívida).

Referência: [EF Core — Pagination](https://learn.microsoft.com/ef/core/querying/pagination) · [Microsoft REST API Guidelines — collections](https://github.com/microsoft/api-guidelines)
