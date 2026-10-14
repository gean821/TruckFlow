---
title: IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant
impact: CRITICAL
impactDescription: escape cross-tenant auditável e mínimo
tags: multi-tenant, ignore-query-filters, security, workers
---

## IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant

Há casos legítimos de ler fora do tenant atual: motorista (que não pertence a uma fábrica) reservando vaga, workers que varrem todas as empresas, claims de fila. Esses escapes precisam ser **explícitos no nome** e **curtos**: achar a linha, descobrir o `EmpresaId` dela, e entrar no tenant com `WithTenant` para o resto da operação.

**Incorreto (IgnoreQueryFilters solto no service):**

```csharp
var vaga = await _db.Agendamento.IgnoreQueryFilters()
    .Include(a => a.Grade)                 // tudo daqui pra frente é cross-tenant
    .FirstOrDefaultAsync(a => a.Id == id, ct);
var outras = await _db.Agendamento.IgnoreQueryFilters()
    .Where(a => a.Status == StatusAgendamento.Disponivel).ToListAsync(ct); // lista de TODAS as fábricas
```

**Correto (método nomeado no repositório + escopo de tenant):**

```csharp
// Repositório
public Task<Agendamento?> GetByIdAcrossTenants(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, ct);

// Service
var vaga = await _repo.GetByIdAcrossTenants(dto.AgendamentoId, ct)
    ?? throw new NotFoundException("Vaga não encontrada.");
using var tenantScope = _empresaContext.WithTenant(vaga.EmpresaId);
// a partir daqui: filtros e interceptor valem para a empresa da vaga
```

```sql
-- SQL cru de fila também precisa de IgnoreQueryFilters (FromSql + filtro global = WHERE extra)
SELECT *, xmin FROM "NotificacaoEntrega"
WHERE "Status" = 0 AND "Canal" = 2
ORDER BY "CreatedAt" FOR UPDATE SKIP LOCKED LIMIT 1;
```

Revisão de PR: todo `IgnoreQueryFilters()` novo precisa de (1) sufixo `AcrossTenants` ou ser claim de worker, (2) justificativa no PR, (3) teste cross-tenant.

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment` (padrão correto), `ObterPorChaveAcrossTenantsAsync` (NF-e), `NotificacaoEntregaRepositorio.ClaimNextPushPendenteAsync`, `AgendamentoExpirationWorker` (itera empresas com `WithTenant`).

Referência: [EF Core — Disabling filters](https://learn.microsoft.com/ef/core/querying/filters#disabling-filters)
