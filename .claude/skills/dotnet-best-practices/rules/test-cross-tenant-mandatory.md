---
title: Toda entidade IEmpresaScoped nova ganha teste cross-tenant
impact: CRITICAL
impactDescription: rede de proteção que impede refactor futuro de reintroduzir vazamento entre fábricas
tags: testing, multi-tenant, security, integration, bola
---

## Toda entidade IEmpresaScoped nova ganha teste cross-tenant

ADR-0004 item 14: "sem isso o blueprint multi-tenant fica sem rede de proteção — qualquer refactor pode reintroduzir vazamento entre empresas, e isso queima contrato Aurora". O teste mínimo por entidade/feature nova cria dados nas Empresas A e B e prova, **com Postgres real**:

1. Listagem como B não contém nada de A.
2. `GetById` de registro de A como B → `NotFoundException` (não 403).
3. Update/Delete de registro de A como B → `NotFoundException` e o registro de A continua intacto.
4. Contexto **sem tenant** → exceção ou zero linhas, nunca "tudo".

**Incorreto (só testa o caminho feliz de um tenant):**

```csharp
[Fact]
public async Task GetPaged_RetornaTransportadoras()
{
    // seed em uma empresa, lista na mesma empresa, Assert.NotEmpty — não prova isolamento
}
```

**Correto:**

```csharp
[Fact]
public async Task GetById_DeRegistroDeOutraEmpresa_LancaNotFound()
{
    var (empresaA, empresaB) = (await SeedEmpresaAsync("A"), await SeedEmpresaAsync("B"));
    Guid idDeA;

    var ctxA = ServiceFactory.NewEmpresaContext();
    using (ctxA.WithTenant(empresaA))
    await using (var dbA = Fixture.CreateDbContext(ctxA))
    {
        var svcA = ServiceFactory.CreateTransportadoraService(dbA, ctxA, new FakeCurrentUserService { EmpresaId = empresaA });
        idDeA = (await svcA.Create(new() { RazaoSocial = "Da A", Cnpj = "11111111000111" }, default)).Id;
    }

    var ctxB = ServiceFactory.NewEmpresaContext();
    using var scopeB = ctxB.WithTenant(empresaB);
    await using var dbB = Fixture.CreateDbContext(ctxB);
    var svcB = ServiceFactory.CreateTransportadoraService(dbB, ctxB, new FakeCurrentUserService { EmpresaId = empresaB });

    await Assert.ThrowsAsync<NotFoundException>(() => svcB.GetById(idDeA, default));
    var paginaB = await svcB.GetPaged(new() { PageNumber = 1, PageSize = 50 }, default);
    Assert.DoesNotContain(paginaB.Items, x => x.Id == idDeA);
}
```

Para `Usuario` (não escopado pelo filtro), o teste é ainda mais importante — ver `tenant-usuario-manual-check`. Evolução recomendada: uma suíte HTTP com `WebApplicationFactory<Program>` + Testcontainers que bate nos endpoints com JWT de A e B e verifica 404/403 por role — cobre a camada que os testes de service não veem (atributos `[Authorize]`, binding, middleware).

**No TruckFlow:** referência `TruckFlow.Test/Integration/NotificacaoCrossTenantTests.cs`.

Referência: [OWASP API1:2023 — BOLA (how to prevent: tests)](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/) · [Integration tests in ASP.NET Core (WebApplicationFactory)](https://learn.microsoft.com/aspnet/core/test/integration-tests)
