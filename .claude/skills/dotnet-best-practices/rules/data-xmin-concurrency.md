---
title: Recurso disputado usa xmin como token de concorrência otimista
impact: HIGH
impactDescription: 100 motoristas na mesma vaga = exatamente 1 reserva (provado em teste)
tags: concurrency, optimistic-locking, xmin, postgres, ef-core
---

## Recurso disputado usa xmin como token de concorrência otimista

Qualquer entidade que dois atores podem alterar ao mesmo tempo com regra "o primeiro ganha" (vaga de agendamento, item de planejamento, notificação, entrega, outbox, dispositivo) usa a coluna de sistema `xmin` do Postgres como concurrency token — sem coluna extra, sem trigger. O EF inclui `WHERE xmin = @original` no UPDATE; zero linhas afetadas → `DbUpdateConcurrencyException`, que o service traduz (ver `error-map-postgres-sqlstate`).

**Incorreto (last write wins):**

```csharp
var vaga = await _repo.GetById(id, ct);
if (vaga!.StatusAgendamento != StatusAgendamento.Disponivel) throw new BusinessException("Indisponível");
vaga.Reservar(motoristaId);          // entre o if e o UPDATE, outro motorista reservou
await _repo.Update(vaga, ct);        // sobrescreve sem erro
```

**Correto:**

```csharp
// Infra/Database/EntitiesMapping/TransportadoraConfiguracao.cs
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

```csharp
try
{
    vaga.Reservar(motoristaId);
    await _repo.Update(vaga, ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

Combine camadas: `xmin` (otimista), exclusion/unique constraint (invariante no banco, ver `postgres-best-practices`) e checagem prévia só para mensagem amigável. Para fila de trabalho entre instâncias, otimista não serve — use `FOR UPDATE SKIP LOCKED` (ver `async-skip-locked-claim`).

**No TruckFlow:** `AgendamentoConfiguracao`, migration `20260510025303_concurrency`; prova em `TruckFlow.Test/Integration/AgendamentoReservaConcorrenciaTests.cs` (100 threads → 1 sucesso, 99 falhas).

Referência: [Npgsql EF — Concurrency tokens (xmin)](https://www.npgsql.org/efcore/modeling/concurrency.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)
