---
title: Factory monta a entidade nova completa — estática e pura quando possível
impact: MEDIUM
impactDescription: construção num lugar só, testável sem banco, sem entidade "meio preenchida"
tags: factory, ddd, creation, entity
---

## Factory monta a entidade nova completa — estática e pura quando possível

O service resolve dependências (tenant, entidades relacionadas já carregadas e validadas) e entrega tudo para a factory, que só **monta**. Factory estática pura (`GradeFactory.Create(dto, empresaId, produto, ...)`) é a preferida: testa em teste de domínio, sem DI. Factory injetável (`ProdutoFactory(ILocalDescargaRepositorio)`) só se justifica quando a montagem inevitavelmente precisa consultar algo — e mesmo assim, prefira o service carregar e passar.

Responsabilidades da factory: preencher `EmpresaId`, `CreatedAt = DateTime.UtcNow` (não existe default no banco nem interceptor para `CreatedAt`), FKs **e** navegações coerentes, status inicial. Se há invariante de criação, ela lança `DomainException`.

**Incorreto:**

```csharp
// service montando na mão, esquecendo CreatedAt
var vaga = new Agendamento { TipoCarga = dto.TipoCarga, LocalDescargaId = dto.LocalDescargaId };
await _repo.AddAgendamento(vaga, ct);      // CreatedAt = 0001-01-01
```

**Correto:**

```csharp
public static class TransportadoraFactory
{
    public static Transportadora Create(TransportadoraCreateDto dto, Guid empresaId) => new()
    {
        RazaoSocial = dto.RazaoSocial.Trim(),
        Cnpj = dto.Cnpj,
        EmpresaId = empresaId,
        CreatedAt = DateTime.UtcNow,
    };
}

// service
var empresaId = _currentUser.GetEmpresaId();
var entidade = TransportadoraFactory.Create(dto, empresaId);
```

Factory estática **não** precisa de `AddTransient<XFactory>()`.

**No TruckFlow:** `Application/Factories/GradeFactory.cs` (referência). Dívidas: `AgendamentoFactory.cs` inteiro comentado; `AgendamentoAdminService.CreateAvulso` não seta `CreatedAt`. Caminho de evolução: mover a montagem para um `static Create(...)` na própria entidade (Evans/Vernon — factory method no agregado) quando o agregado ganhar setters privados.

Referência: Eric Evans, *DDD* — Factories · Vaughn Vernon, *Implementing Domain-Driven Design* — cap. 11 · [Steve Smith — Ardalis.GuardClauses](https://github.com/ardalis/GuardClauses)
