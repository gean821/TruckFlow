---
title: Concorrência otimista com xmin; DbUpdateConcurrencyException vira BusinessException
impact: CRITICAL
impactDescription: 100 reservas simultâneas na mesma vaga → exatamente 1 sucesso, sem lock pessimista
tags: optimistic-concurrency, xmin, concurrency-token, ef-core, npgsql
---

## Concorrência otimista com xmin; DbUpdateConcurrencyException vira BusinessException

Read-modify-write sem controle de versão perde atualização: dois motoristas leem a vaga `Disponivel`, ambos setam `Agendado`, o último ganha. A coluna de sistema `xmin` do Postgres muda a cada UPDATE — mapeada como concurrency token, o EF gera `UPDATE ... WHERE "Id" = @id AND xmin = @original` e lança `DbUpdateConcurrencyException` se 0 linhas mudarem. Sem coluna extra, sem trigger.

**Incorreto (last write wins):**

```csharp
var vaga = await _db.Agendamento.FirstAsync(a => a.Id == id, ct);
if (vaga.StatusAgendamento != StatusAgendamento.Disponivel) throw new BusinessException("Vaga indisponível.");
vaga.Reservar(...);
await _db.SaveChangesAsync(ct);   // UPDATE ... WHERE "Id" = @id — sobrescreve a reserva do outro
```

```sql
UPDATE "Agendamento" SET "StatusAgendamento" = 2, "UsuarioId" = $1 WHERE "Id" = $2;
```

**Correto (token + tradução para erro de negócio):**

```csharp
// AgendamentoConfiguracao.cs (padrão real)
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();

// Service
try
{
    await _repo.Update(vaga, ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

```sql
UPDATE "Agendamento" SET "StatusAgendamento" = 2, "UsuarioId" = $1
WHERE "Id" = $2 AND xmin = $3;   -- 0 linhas → DbUpdateConcurrencyException
```

Regras: **não** faça retry automático cego (a regra de negócio precisa ser reavaliada com dado novo); `DbUpdateConcurrencyException` não tratada vira 500 no `ExceptionHandlingMiddleware` — sempre traduza; SQL cru que lê entidade com xmin precisa selecionar `xmin` (`SELECT *, xmin FROM ...`); `ExecuteUpdateAsync` **ignora** o token.

Quando o conflito é frequente e o custo de refazer é alto (fila), prefira `FOR UPDATE SKIP LOCKED` (`lock-skip-locked`).

**No TruckFlow:** `xmin` em `Agendamento`, `ItemPlanejamento`, `Notificacao`, `NotificacaoEntrega`, `OutboxEvent`, `DispositivoUsuario` (migration `20260510025303_concurrency`). Coberto por `Integration/AgendamentoReservaConcorrenciaTests` (100 threads → 1 sucesso).

Referência: [Npgsql — Optimistic concurrency with xmin](https://www.npgsql.org/efcore/modeling/concurrency.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency) · [System Columns](https://www.postgresql.org/docs/16/ddl-system-columns.html)
