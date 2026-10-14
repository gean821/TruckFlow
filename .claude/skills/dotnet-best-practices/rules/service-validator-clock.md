---
title: Validator e regra de tempo usam TimeProvider em UTC, avaliado na hora
impact: HIGH
impactDescription: "data já passou" deixa de depender do fuso do servidor e da hora em que o validator foi construído
tags: datetime, utc, timeprovider, fluentvalidation, testing
---

## Validator e regra de tempo usam TimeProvider em UTC, avaliado na hora

`GreaterThan(DateTime.Now.AddMinutes(-5))` tem dois bugs: (1) `DateTime.Now` é hora **local do servidor** (Railway/Azure = UTC, máquina do dev = BRT) enquanto o domínio grava UTC; (2) o argumento é avaliado **uma vez**, quando o validator é construído — com lifetime Singleton ou cache, a "agora" congela. Use `TimeProvider` (.NET 8), avaliado dentro de `Must`, e compare UTC com UTC. Bônus: em teste você injeta `FakeTimeProvider` e testa a virada de janela sem `Thread.Sleep`.

**Incorreto:**

```csharp
RuleFor(x => x.DataInicio)
    .GreaterThan(DateTime.Now.AddMinutes(-5))     // local + congelado na construção
    .WithMessage("A data informada já passou.");
```

**Correto:**

```csharp
public sealed class AgendamentoAdminCreateValidator : AbstractValidator<AgendamentoAdminCreateDto>
{
    public AgendamentoAdminCreateValidator(TimeProvider clock)
    {
        RuleFor(x => x.DataInicio)
            .Must(d => d.ToUniversalTime() > clock.GetUtcNow().UtcDateTime.AddMinutes(-5))
            .WithMessage("A data informada já passou.");
    }
}

// Program.cs
builder.Services.AddSingleton(TimeProvider.System);
```

Regra geral de tempo: persistir e comparar em **UTC**; converter para `Grade.OperationalTimeZone` (America/Sao_Paulo) só na borda (montar slots do dia, exibir). Nunca `DateTime.Now` em código de servidor.

**No TruckFlow:** dívida em `Validators/AgendamentoAdmin/AgendamentoAdminCreateValidator.cs`. Entidades e services hoje usam `DateTime.UtcNow` direto — ok; ao tocar numa regra temporal testável, migre aquele ponto para `TimeProvider`.

Referência: [.NET 8 — TimeProvider](https://learn.microsoft.com/dotnet/standard/datetime/timeprovider-overview) · [Microsoft.Extensions.TimeProvider.Testing — FakeTimeProvider](https://www.nuget.org/packages/Microsoft.Extensions.TimeProvider.Testing) · [Npgsql — Date and time handling (timestamptz = UTC)](https://www.npgsql.org/doc/types/datetime.html)
