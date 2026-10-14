---
title: IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant
impact: CRITICAL
impactDescription: torna cada furo de isolamento explícito, nomeado e revisável em PR
tags: multi-tenant, ef-core, query-filter, repository
---

## IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant

Há fluxos legítimos que cruzam tenant — o motorista não pertence a uma fábrica e reserva vaga em qualquer uma. O padrão é: o repositório expõe um método com sufixo `AcrossTenants` (único lugar com `IgnoreQueryFilters()`), e o service, logo após descobrir o tenant da linha, abre `using var scope = _empresaContext.WithTenant(x.EmpresaId)` para que o resto do fluxo (incluindo escrita e interceptors) rode no tenant correto.

**Incorreto (furo silencioso num método comum):**

```csharp
public Task<Agendamento?> GetById(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);
```

**Correto:**

```csharp
// Repositório: o nome declara a intenção
public Task<Agendamento?> GetByIdAcrossTenants(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == id, ct);

// Service
var vaga = await _repo.GetByIdAcrossTenants(dto.AgendamentoId, ct)
    ?? throw new NotFoundException("Vaga não encontrada.");
using var tenantScope = _empresaContext.WithTenant(vaga.EmpresaId);
// daqui pra baixo, filtros e interceptor usam vaga.EmpresaId
```

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment` e `ObterPorChaveAcrossTenantsAsync` (NotaFiscal) seguem isso. Em code review, `IgnoreQueryFilters` fora de método `*AcrossTenants` (ou de teste) é bloqueante.

Referência: [EF Core — Disabling filters](https://learn.microsoft.com/ef/core/querying/filters#disabling-filters)
