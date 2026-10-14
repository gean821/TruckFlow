---
title: Service devolve DTO, mapeado à mão com private static MapToResponse
impact: HIGH
impactDescription: contrato estável, sem lazy-load acidental nem vazamento de campo interno, checado em compile-time
tags: dto, mapping, service, automapper, clean-architecture
---

## Service devolve DTO, mapeado à mão com private static MapToResponse

Entidade EF fora do service vaza navegação (ciclo na serialização, N+1 por lazy load), campos internos (`xmin`, `CreatedBy`, hash de senha do Identity) e acopla o contrato público ao schema. A convenção da casa: **toda** assinatura pública de service retorna `XResponse`/`XResponseDto` (ou `PagedResponse<XResponse>`), e a conversão é um `private static XResponse MapToResponse(Entity e)` no fim do service.

Por que mapeamento manual e não AutoMapper: o compilador pega campo renomeado, "Find usages" funciona, não há reflexão nem convenção mágica escondendo `null`. O próprio autor do AutoMapper (Jimmy Bogard) diz que ele **não** foi feito para mapear para DTO com lógica (fallbacks, enum → string). Se o volume de mapeamento crescer muito, a evolução aceitável é **Mapperly** (source generator, zero reflexão) — com ADR, não ad hoc.

**Incorreto:**

```csharp
public async Task<Agendamento> GetById(Guid id, CancellationToken ct)
    => await _repo.GetById(id, ct);                       // entidade EF no contrato
```

**Correto (padrão `AgendamentoAdminService`):**

```csharp
public async Task<AgendamentoAdminResponse> GetById(Guid id, CancellationToken ct)
{
    var agendamento = await _repo.GetById(id, ct)
        ?? throw new NotFoundException("Agendamento não encontrado.");
    return MapToResponse(agendamento);
}

private static AgendamentoAdminResponse MapToResponse(Agendamento agendamento) => new()
{
    Id = agendamento.Id,
    FornecedorNome = agendamento.Fornecedor?.Nome,
    MotoristaNome = agendamento.Usuario?.Motorista?.NomeReal,
    DataInicio = agendamento.DataInicio,
    DataFim = agendamento.DataFim,
    PesoCarga = agendamento.VolumeCarga ?? agendamento.NotaFiscal?.PesoBruto ?? 0,
    PlacaVeiculo = agendamento.PlacaVeiculo ?? agendamento.NotaFiscal?.PlacaVeiculo,
    Status = agendamento.StatusAgendamento.ToString(),
    CreatedAt = agendamento.CreatedAt,
};
```

Regras do DTO de saída: só o que a tela precisa; enum como string quando o front exibe; telefone já sanitizado (só dígitos com DDI, ADR-0002); nada de `EmpresaId` de outro tenant; `MapToResponse` puro (sem I/O, sem `DateTime.Now`).

Nomenclatura para código novo: `XCreateDto`, `XUpdateDto`, `XListQueryDto`, `XResponse` (é o mais comum hoje). Não renomeie os `*ResponseDto` existentes por estética.

**No TruckFlow:** 13 services com `MapToResponse`. Referência: `TruckFlow.Application/AgendamentoAdminService.cs` (L460-481), `GradeService`.

Referência: [Jimmy Bogard — AutoMapper usage guidelines](https://www.jimmybogard.com/automapper-usage-guidelines/) · [Mapperly](https://mapperly.riok.app/) · [Martin Fowler — Data Transfer Object](https://martinfowler.com/eaaCatalog/dataTransferObject.html)
