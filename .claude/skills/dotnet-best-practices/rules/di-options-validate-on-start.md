---
title: Configuração via Options tipadas com ValidateOnStart
impact: MEDIUM
impactDescription: chave JWT vazia ou certificado A1 ausente derruba o deploy, não o primeiro request em produção
tags: options-pattern, configuration, startup, secrets
---

## Configuração via Options tipadas com ValidateOnStart

Ler `configuration["Sefaz:Certificado:Senha"]` espalhado pelo código esconde dependência e só falha quando o fluxo roda. Use classe de options + `BindConfiguration` + validação + `ValidateOnStart()`: config inválida faz a aplicação **não subir** — o healthcheck do Railway (`/health/ready`) segura o deploy e a versão anterior continua no ar.

**Incorreto:**

```csharp
var key = builder.Configuration["JwtOptions:SecurityKey"];     // "" em appsettings → assina com chave vazia?
var senha = _config["Sefaz:Certificado:Senha"];                 // descobre no primeiro consumo
```

**Correto:**

```csharp
public sealed class JwtOptions
{
    public const string Section = "JwtOptions";
    [Required, MinLength(64)] public string SecurityKey { get; init; } = "";
    [Required] public string Issuer { get; init; } = "TruckFlow";
    [Required] public string Audience { get; init; } = "TruckFlow";
}

services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.Section)
    .ValidateDataAnnotations()
    .ValidateOnStart();

services.AddOptions<SefazOptions>()
    .BindConfiguration("Sefaz")
    .Validate(o => o.UseFake || !string.IsNullOrWhiteSpace(o.Certificado.Thumbprint)
                             || !string.IsNullOrWhiteSpace(o.Certificado.Caminho),
              "Sefaz: informe Certificado.Thumbprint ou Certificado.Caminho quando UseFake=false.")
    .ValidateOnStart();
```

Consuma via `IOptions<T>` (config estática) ou `IOptionsMonitor<T>` (recarregável). Segredo nunca tem default real no código nem no `appsettings.json` — vem de user-secrets (dev) e env vars `Secao__Chave` (prod).

**No TruckFlow:** `SefazOptions`, `ResendOptions`, `GeocodingOptions` já existem como classes — falta `ValidateOnStart`. `appsettings` tem `JwtOptions:SecurityKey` vazio esperando env/user-secrets — exatamente o caso que `ValidateOnStart` protege.

Referência: [Options pattern in .NET — validation](https://learn.microsoft.com/dotnet/core/extensions/options#options-validation) · [Andrew Lock — Adding validation to strongly typed configuration](https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-dotnet-6/)
