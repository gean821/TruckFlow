---
title: Toda entidade de tenant implementa IEmpresaScoped — o filtro global falha fechado
impact: CRITICAL
impactDescription: elimina vazamento entre empresas por esquecimento de Where
tags: multi-tenant, query-filter, empresa-scoped, security, ef-core
---

## Toda entidade de tenant implementa IEmpresaScoped — o filtro global falha fechado

Isolamento por `Where(x => x.EmpresaId == empresaId)` manual depende de ninguém esquecer. No TruckFlow o isolamento é **estrutural**: a entidade implementa `IEmpresaScoped`, o `AppDbContext` aplica `HasQueryFilter` por reflexão e o `EmpresaScopedSaveChangesInterceptor` bloqueia escrita cross-tenant. Sem tenant no contexto, a query **lança** (falha fechada) — nunca devolve tudo.

**Incorreto (entidade nova sem a interface; filtro manual):**

```csharp
public class Transportadora : EntidadeBase   // esqueceu IEmpresaScoped
{
    public Guid EmpresaId { get; set; }
}
// service
var lista = await _db.Transportadora.Where(t => t.EmpresaId == empresaId).ToListAsync(ct); // o próximo dev esquece
```

```sql
-- o que vaza quando alguém esquece o Where:
SELECT * FROM "Transportadora";  -- todas as 21 fábricas
```

**Correto (interface + filtro global + índice com EmpresaId):**

```csharp
public class Transportadora : EntidadeBase, IEmpresaScoped
{
    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
// AppDbContext.SetGlobalFilter<TEntity> já aplica:
//   .HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)
var lista = await _db.Transportadora.ToListAsync(ct); // SQL sai com WHERE "EmpresaId" = @__ef_filter__EmpresaId
```

```sql
-- SQL gerado
SELECT t.* FROM "Transportadora" AS t WHERE t."EmpresaId" = @__ef_filter__EmpresaId_0;
```

**Não regredir o fail-closed (ADR-0004 item 6):** o bug histórico era o fallback do filtro retornar `true` quando `EmpresaId == Guid.Empty`. Hoje `EmpresaContext.EmpresaId` lança `UnauthorizedAccessException("Tenant não identificado.")` quando não há claim nem override. Nunca troque isso por `?? Guid.Empty`, `|| empresaId == Guid.Empty` ou similar.

Teste obrigatório para entidade nova (Postgres real, `TruckFlow.Test/Integration`): criar dados em A e B, consultar como B → vazio; `GetById` de registro de A como B → `NotFoundException`.

**No TruckFlow:** `TruckFlow.Domain/Contracts/IEmpresaScoped.cs`, `AppDbContext.SetGlobalFilter`, `Database/Interceptors/EmpresaScopedSaveChangesInterceptor.cs`, `TruckFlow.Domain/Entities/EmpresaContext.cs`. Exceção conhecida: `Usuario` (Identity) **não** é `IEmpresaScoped` — toda query de usuário filtra `EmpresaId` à mão (e há gaps em `UsuarioService.SetAdminStatusAsync`/`GetAdminByIdAsync`).

Referência: [EF Core — Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters) · [Microsoft — Multi-tenancy with EF Core](https://learn.microsoft.com/ef/core/miscellaneous/multitenancy)
