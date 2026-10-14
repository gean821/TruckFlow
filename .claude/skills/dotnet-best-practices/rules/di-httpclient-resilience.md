---
title: HttpClient tipado via IHttpClientFactory com resilience handler padrão
impact: MEDIUM-HIGH
impactDescription: Expo/Resend/geocoding instáveis não derrubam o worker nem esgotam sockets
tags: httpclient, resilience, polly, retry, circuit-breaker, integrations
---

## HttpClient tipado via IHttpClientFactory com resilience handler padrão

`new HttpClient()` por chamada esgota sockets; `HttpClient` estático ignora troca de DNS. O projeto já usa `AddHttpClient<IExpoPushClient, ExpoPushClient>` (typed) e named clients para geocoding — mas **sem nenhuma política de resiliência**, só timeout. Em .NET 8 o caminho oficial é `Microsoft.Extensions.Http.Resilience` (Polly v8): `AddStandardResilienceHandler()` aplica rate limiter, timeout total, retry com backoff exponencial + jitter, circuit breaker e timeout por tentativa.

**Incorreto:**

```csharp
var http = new HttpClient();
var resp = await http.PostAsJsonAsync("https://exp.host/--/api/v2/push/send", msgs, ct);
```

**Correto:**

```csharp
services.AddHttpClient<IExpoPushClient, ExpoPushClient>(c =>
    {
        c.BaseAddress = new Uri("https://exp.host");
    })
    .AddStandardResilienceHandler(o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        o.Retry.MaxRetryAttempts = 3;
    });
```

Cuidados:
- **Retry só em operação idempotente** ou com chave de idempotência. POST de push em lote: o worker já tem retry com backoff via `NotificacaoEntrega` — evite retry duplo (desligue o retry do handler e mantenha circuit breaker/timeout, ou vice-versa, e documente).
- Circuit breaker aberto = falha rápida; o worker loga `Warning` e reagenda.
- SEFAZ via Zeus **não** usa HttpClient (lib própria, síncrona) — resiliência lá é timeout + `Task.Run` com cancelamento + fallback de UX (ver `async-sync-library-wrap`).

**No TruckFlow:** `Extensions/Notificacao` (Expo), `Extensions/Email` (Resend), `Extensions/Geocoding` (Nominatim/Google).

Referência: [Build resilient HTTP apps (Microsoft.Extensions.Http.Resilience)](https://learn.microsoft.com/dotnet/core/resilience/http-resilience) · [IHttpClientFactory guidelines](https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines) · [Polly v8](https://www.pollydocs.org/)
