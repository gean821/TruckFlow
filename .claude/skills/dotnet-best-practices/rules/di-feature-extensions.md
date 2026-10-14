---
title: Uma extension AddX() por feature, chamada no Program.cs, sem registro duplicado
impact: MEDIUM
impactDescription: feature nova não "funciona no meu teste e quebra em runtime" por DI faltando
tags: dependency-injection, extensions, program-cs, composition-root
---

## Uma extension AddX() por feature, chamada no Program.cs, sem registro duplicado

Convenção: `TruckFlow/Extensions/<Feature>/<Feature>DependencyInjection.cs` com `public static IServiceCollection Add<Feature>(this IServiceCollection services)`, registrando service, repositório, factory injetável e validators da feature, e chamada uma vez no `Program.cs`. O nome do método **é o nome da feature** — copiar a extension de outra feature e esquecer de renomear o método gera um `AddProduto` duplicado que nunca é chamado.

Repositório compartilhado (ex.: `IProdutoRepositorio` usado por Grade, Empresa, Fornecedor) é registrado **só na feature dona**. Se outra feature precisar garantir presença, use `TryAddScoped` — nunca `AddTransient` de novo.

**Incorreto:**

```csharp
// Extensions/Veiculo/VeiculoDependencyInjection.cs
public static IServiceCollection AddProduto(this IServiceCollection services)   // nome copiado
{
    services.AddTransient<IVeiculoService, VeiculoService>();
    services.AddTransient<IProdutoRepositorio, ProdutoRepositorio>();          // duplicado de novo
    return services;
}
// Program.cs nunca chama → VeiculoController falha ao ativar
```

**Correto:**

```csharp
namespace TruckFlow.Extensions.Veiculo;

public static class VeiculoDependencyInjection
{
    public static IServiceCollection AddVeiculo(this IServiceCollection services)
    {
        services.AddScoped<IVeiculoService, VeiculoService>();
        services.AddScoped<IVeiculoRepositorio, VeiculoRepositorio>();
        services.AddScoped<IValidator<VeiculoCreateDto>, VeiculoCreateValidator>();
        return services;
    }
}

// Program.cs
builder.Services.AddVeiculo();
```

Rede de segurança barata: `builder.Host.UseDefaultServiceProvider(o => { o.ValidateScopes = true; o.ValidateOnBuild = true; });` em Development — falha no startup se faltar registro ou houver captive dependency.

**No TruckFlow:** bug real em `Extensions/Veiculo/VeiculoDependencyInjection.cs` (`AddProduto`, nunca chamado). Duplicatas: `IProdutoRepositorio` em Grade/Empresa/Fornecedor, `ILocalDescargaRepositorio` em Produto/LocalDescarga, `IFornecedorRepositorio` em Grade/Fornecedor. Typo `AddItemPlanejameto()`.

Referência: [Dependency injection guidelines](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection-guidelines) · [Andrew Lock — ValidateOnBuild / ValidateScopes](https://andrewlock.net/new-in-asp-net-core-3-service-provider-validation/)
