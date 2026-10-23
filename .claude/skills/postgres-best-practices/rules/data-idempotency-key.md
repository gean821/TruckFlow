---
title: Idempotency-Key em POST críticos (reservar, posição) — padrão Stripe com unique no banco
impact: HIGH
impactDescription: retry de rede/double-tap nunca duplica reserva ou batch de posição
tags: idempotency, idempotency-key, retries, stripe, unique-constraint
---

## Idempotency-Key em POST críticos (reservar, posição) — padrão Stripe com unique no banco

Motorista em rede ruim toca "Reservar", o request chega, a resposta se perde, o app tenta de novo. Sem idempotência, o segundo request pode criar efeito duplicado ou devolver erro confuso ("vaga já reservada" — por ele mesmo). O cliente manda `Idempotency-Key: <uuid>`; o servidor grava a chave **na mesma transação** do efeito e, num replay, devolve a resposta original.

**Incorreto:**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar([FromBody] ReservarAgendamentoDto dto, CancellationToken ct)
    => Ok(await _service.BookAppointment(dto, ct));   // replay = segundo efeito ou erro enganoso
```

**Correto (tabela + unique + mesma transação):**

```sql
CREATE TABLE "IdempotencyKey" (
    "Key"            uuid        NOT NULL,
    "UsuarioId"      uuid        NOT NULL,
    "Rota"           varchar(120) NOT NULL,
    "RequestHash"    char(64)    NOT NULL,          -- sha256 do body: mesma chave + body diferente = 422
    "StatusCode"     int         NULL,
    "ResponseBody"   jsonb       NULL,
    "CriadoEm"       timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY ("UsuarioId", "Key")                -- escopo por usuário: chave de um não colide com outro
);
CREATE INDEX "IX_IdempotencyKey_CriadoEm" ON "IdempotencyKey" ("CriadoEm"); -- limpeza > 24h
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var existente = await _db.IdempotencyKey.FindAsync([userId, key], ct);
if (existente is { StatusCode: not null })
    return existente.RequestHash == hash ? Replay(existente) : throw new BusinessException("Idempotency-Key reutilizada com outro conteúdo.");

var registro = new IdempotencyKey { Key = key, UsuarioId = userId, Rota = "reservar", RequestHash = hash };
_db.IdempotencyKey.Add(registro);
var resultado = await _service.BookAppointment(dto, ct);          // efeito
registro.StatusCode = 200; registro.ResponseBody = JsonSerializer.Serialize(resultado);
await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);   // chave e efeito commitam juntos; requisição concorrente com a mesma chave → 23505 → 409 "em processamento"
```

Implementar como `IEndpointFilter`/action filter reutilizável, aplicado por atributo (`[Idempotent]`). Retenção 24h com job de limpeza (`DELETE ... WHERE "CriadoEm" < now() - interval '24 hours'` em lote). Domain events já têm `IdempotencyKey` no outbox (unique em `OutboxEventConfiguracao.cs:57`) — mesmo princípio no consumo.

**No TruckFlow:** ADR-0004 item 12 (pré-piloto) — `/AgendamentoMotorista/reservar` e `POST /v1/motorista/posicao`. Mobile gera o uuid por **intenção** (mesma chave nos retries), não por tentativa.

Referência: [Brandur — Implementing Stripe-like Idempotency Keys in Postgres](https://brandur.org/idempotency-keys) · [Stripe — Idempotent requests](https://docs.stripe.com/api/idempotent_requests) · [IETF draft — Idempotency-Key header](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)
