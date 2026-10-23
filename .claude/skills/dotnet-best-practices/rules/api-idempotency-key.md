---
title: Idempotency-Key em POSTs que o cliente pode repetir
impact: HIGH
impactDescription: retry de rede do mobile não duplica reserva nem posição
tags: idempotency, api-design, reliability, mobile
---

## Idempotency-Key em POSTs que o cliente pode repetir

Motorista no pátio com 3G: o `POST /reservar` sai, o servidor processa, a resposta se perde, o app (ou o usuário) tenta de novo. Sem idempotência, o segundo request ou cria uma segunda escrita ou devolve um erro falso ("vaga já reservada" — por ele mesmo). ADR-0004 item 12 torna isso pré-piloto para `/reservar` e `POST /motorista/posicao`.

Padrão (Stripe): cliente gera um UUID por **intenção** (não por tentativa) e envia `Idempotency-Key`. Servidor grava `(EmpresaId|MotoristaId, Key, RequestHash, StatusCode, ResponseBody, CriadoEm)` com unique em `(Escopo, Key)`, dentro da mesma transação do efeito. Repetição com mesma chave e mesmo hash → devolve a resposta salva; mesma chave com payload diferente → 422. Retenção 24h.

**Incorreto:**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar([FromBody] ReservarAgendamentoDto dto, CancellationToken ct)
    => Ok(await _service.BookAppointment(dto, ct));    // retry = segunda tentativa de reserva
```

**Correto (esboço):**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar(
    [FromBody] ReservarAgendamentoDto dto,
    [FromHeader(Name = "Idempotency-Key")] Guid? idempotencyKey,
    CancellationToken ct)
{
    var result = await _service.BookAppointment(dto, idempotencyKey, ct);
    return Ok(result);
}

// service
if (idempotencyKey is { } key)
{
    var anterior = await _idempotencia.ObterAsync(_currentUser.GetMotoristaId(), key, ct);
    if (anterior is not null) return anterior.Deserialize<AgendamentoMotoristaResponse>();
}
// ... efeito + gravação da chave na MESMA transação; 23505 na chave = corrida com a própria repetição → reler e devolver
```

Prefira tabela no Postgres a Redis enquanto não houver Redis na stack (menos uma peça para a Aurora auditar). Comece opcional (header ausente = comportamento atual) para não quebrar o mobile já publicado.

**No TruckFlow:** domain events já carregam `IdempotencyKey` e o lifecycle tem "reserva idempotente" — reaproveite a ideia na borda HTTP.

Referência: [Stripe — Idempotent requests](https://docs.stripe.com/api/idempotent_requests) · [IETF draft — The Idempotency-Key HTTP Header Field](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/) · [Gregor Hohpe — Idempotent Receiver (EIP)](https://www.enterpriseintegrationpatterns.com/patterns/messaging/IdempotentReceiver.html)
