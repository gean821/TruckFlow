---
title: Não engula exceção — trate, traduza ou deixe subir
impact: CRITICAL
impactDescription: evita estado inconsistente silencioso (reserva sem evento, nota sem item)
tags: exceptions, logging, reliability
---

## Não engula exceção — trate, traduza ou deixe subir

Um `catch` só se justifica se faz uma de três coisas: **recupera** (retry, fallback documentado), **traduz** (Postgres → `BusinessException`) ou **enriquece e relança** (log com contexto + `throw;`). `catch { }` ou `catch (Exception) { return null; }` esconde bug de produção e, num sistema fiscal, gera dado que ninguém consegue auditar depois.

**Incorreto:**

```csharp
try { await _sefaz.ConsultarDistribuicaoAsync(chave, ct); }
catch (Exception) { return null; }   // SEFAZ fora? certificado vencido? ninguém sabe

try { await _push.SendAsync(msg, ct); }
catch { }                            // e o NotificacaoEntrega fica "Pendente" para sempre
```

**Correto:**

```csharp
try
{
    return await _sefaz.ConsultarDistribuicaoAsync(chave, ct);
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogWarning(ex, "SEFAZ indisponível ao buscar nota {ChaveFinal}", chave[^6..]);
    throw new BusinessException("SEFAZ indisponível no momento. Envie o XML da nota ou tente em alguns minutos.");
}
```

Para relançar a mesma exceção use `throw;` (preserva stack), nunca `throw ex;`.

Exceções toleradas de "best effort" (ex.: `signOut` que tenta revogar refresh token) devem **logar** em `Warning` e ter comentário dizendo por que a falha é aceitável.

**No TruckFlow:** workers (`OutboxProcessorWorker`, `AgendamentoExpirationWorker`) fazem certo: capturam por item/empresa, logam com contexto, seguem. `DbUpdateConcurrencyException` no `AgendamentoExpirationService` é logado e deixado para o próximo ciclo — exemplo de "recuperar" bem documentado.

Referência: [Stephen Cleary — exception handling in async](https://blog.stephencleary.com/2012/02/async-and-await.html) · [.NET — Best practices for exceptions](https://learn.microsoft.com/dotnet/standard/exceptions/best-practices-for-exceptions)
