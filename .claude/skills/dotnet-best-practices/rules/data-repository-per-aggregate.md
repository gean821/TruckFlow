---
title: Um repositório sealed por agregado, sem repositório genérico
impact: MEDIUM-HIGH
impactDescription: queries com nome de negócio, Include certo por caso de uso, nada de IQueryable vazando
tags: repository, ef-core, ddd, aggregate
---

## Um repositório sealed por agregado, sem repositório genérico

O `DbContext` já é Unit of Work + Repository genérico (Microsoft Learn e Jon P Smith). Um `IRepository<T>` com `GetAll/Add/Update` por cima só adiciona indireção e esconde as queries que importam. O padrão da casa: `XRepositorio` **sealed**, primary constructor `(AppDbContext db)`, interface `IXRepositorio` em `Infra/Repositories/Interfaces/`, métodos com **nome de caso de uso** (`GetConflitosAsync`, `GetAdminViewAsync`, `ObterPorChaveAcrossTenantsAsync`) e os `Include` que aquele caso precisa.

**Incorreto:**

```csharp
public class Repository<T>(AppDbContext db) : IRepository<T> where T : class
{
    public IQueryable<T> Query() => db.Set<T>();     // regra de query espalhada pelos services
    public Task<List<T>> GetAll() => db.Set<T>().ToListAsync();
}
```

**Correto:**

```csharp
public sealed class TransportadoraRepositorio(AppDbContext db) : ITransportadoraRepositorio
{
    private readonly AppDbContext _db = db;

    public Task<Transportadora?> GetById(Guid id, CancellationToken ct) =>
        _db.Transportadora.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<bool> ExisteCnpjAsync(string cnpj, CancellationToken ct) =>
        _db.Transportadora.AnyAsync(x => x.Cnpj == cnpj, ct);     // filtro de tenant aplicado pelo global filter

    public async Task<Transportadora> Add(Transportadora entidade, CancellationToken ct)
    {
        await _db.Transportadora.AddAsync(entidade, ct);
        await _db.SaveChangesAsync(ct);
        return entidade;
    }
}
```

Um agregado, um repositório: `NotaFiscalItem` é acessado via `NotaFiscal`, não tem repositório de escrita próprio (exceto leitura especializada justificada, como em Conferência). Specification pattern (Ardalis.Specification) é opção se as queries de um agregado explodirem em combinações — decidir por ADR.

**No TruckFlow:** `Infra/Repositories/AgendamentoRepositorio.cs`. Dívida arquitetural conhecida: interfaces de repositório no Infra e Application referenciando Infra (inversão de dependência incompleta) — não corrija de passagem; é refactor com ADR.

Referência: [Microsoft — Design the infrastructure persistence layer](https://learn.microsoft.com/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design) · [Jon P Smith — Is the repository pattern useful with EF Core?](https://www.thereformedprogrammer.net/is-the-repository-pattern-useful-with-entity-framework-core/) · [Steve Smith — Ardalis.Specification](https://github.com/ardalis/Specification) · [Martin Fowler — Repository](https://martinfowler.com/eaaCatalog/repository.html)
