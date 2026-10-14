---
title: Toda entidade de tenant implementa IEmpresaScoped
impact: CRITICAL
impactDescription: o global query filter e o interceptor de escrita passam a proteger a entidade sem código extra
tags: multi-tenant, ef-core, query-filter, entity
---

## Toda entidade de tenant implementa IEmpresaScoped

O isolamento do TruckFlow tem três peças que só funcionam juntas: o marcador `IEmpresaScoped`, o `HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)` aplicado por reflexão em `AppDbContext`, e o `EmpresaScopedSaveChangesInterceptor`, que preenche `EmpresaId` no Added e rejeita escrita em linha de outro tenant. Uma entidade nova com `EmpresaId` mas **sem** o marcador fica fora das três: qualquer `ToListAsync()` devolve dados de todas as fábricas.

**Incorreto (tem EmpresaId, mas não é escopada):**

```csharp
public class Transportadora : EntidadeBase
{
    public required string RazaoSocial { get; set; }
    public Guid EmpresaId { get; set; }   // ninguém filtra isso
    public Empresa? Empresa { get; set; }
}
```

**Correto:**

```csharp
public class Transportadora : EntidadeBase, IEmpresaScoped
{
    public required string RazaoSocial { get; set; }
    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
```

E na configuração, índice começando por `EmpresaId` (toda query filtra por ele):

```csharp
builder.HasIndex(x => new { x.EmpresaId, x.Cnpj }).IsUnique();
```

**No TruckFlow:** padrão em `TruckFlow.Domain/Contracts/IEmpresaScoped.cs`, aplicado a Agendamento, Grade, Fornecedor, Produto, NotaFiscal, Notificacao etc. Toda entidade nova escopada **precisa** de um teste cross-tenant (ver `test-cross-tenant-mandatory`).

Referência: [EF Core — Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters) · [OWASP API1:2023 BOLA](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/)
