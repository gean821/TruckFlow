---
title: Biblioteca síncrona (Zeus/SEFAZ) — isole, limite o tempo e falhe com UX
impact: MEDIUM
impactDescription: SEFAZ lenta não segura threads do pool nem deixa o motorista esperando 60s
tags: async, sefaz, zeus, timeout, integration
---

## Biblioteca síncrona (Zeus/SEFAZ) — isole, limite o tempo e falhe com UX

`Zeus.Net.NFe.NFCe` é síncrona e faz SOAP com certificado A1. `Task.Run(() => ...)` é a única opção para não bloquear o request — mas `Task.Run` **não** cancela trabalho síncrono: o token só evita *iniciar*. Por isso: timeout curto na própria lib, `WaitAsync(timeout, ct)` para devolver a resposta ao usuário a tempo, e fallback claro (o fluxo do mobile cai para upload de XML).

**Incorreto:**

```csharp
public NfeResult Consultar(string chave) => _servicos.NfeConsultaProtocolo(chave);   // bloqueia a thread do request

return await Task.Run(() => ExecutarConsulta(chave, uf, ambiente));   // sem token, sem limite (TimeOut = 60000)
```

**Correto:**

```csharp
public async Task<SefazConsultaResult> ConsultarProtocoloAsync(string chave, string uf, CancellationToken ct)
{
    var ambiente = _options.Ambiente == 1 ? TipoAmbiente.Producao : TipoAmbiente.Homologacao;
    try
    {
        return await Task.Run(() => ExecutarConsulta(chave, uf, ambiente), ct)
            .WaitAsync(TimeSpan.FromSeconds(20), ct);
    }
    catch (TimeoutException)
    {
        _logger.LogWarning("SEFAZ não respondeu em 20s para UF {Uf}", uf);
        throw new BusinessException("SEFAZ demorou para responder. Envie o XML da nota ou tente novamente.");
    }
}
```

Mantenha a abstração `ISefazClient` com `FakeSefazClient` (`Sefaz:UseFake`) para dev/teste — integração real só com certificado e ambiente explícitos. Nunca logue senha/serial do certificado.

**No TruckFlow:** `TruckFlow.Application/Sefaz/ZeusSefazClient.cs` (`TimeOut = 60000`, `Task.Run` com token). Contexto de negócio em `Docs/sefaz-certificado-consulta-nfe.md`; agente `sefaz-nfe-specialist`.

Referência: [Stephen Cleary — Task.Run etiquette](https://blog.stephencleary.com/2013/10/taskrun-etiquette-and-proper-usage.html) · [Task.WaitAsync](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.waitasync) · [David Fowler — Async Guidance (avoid sync over async / async over sync)](https://github.com/davidfowl/AspNetCoreDiagnosticScenarios/blob/master/AsyncGuidance.md)
