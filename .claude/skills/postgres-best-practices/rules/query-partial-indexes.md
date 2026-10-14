---
title: Use índice parcial para filtros fixos (filas, pendentes, ativos)
impact: HIGH
impactDescription: índices 5-20x menores; filas de worker em O(pendentes) e não O(histórico)
tags: indexes, partial-index, queue, workers, hasfilter
---

## Use índice parcial para filtros fixos (filas, pendentes, ativos)

Índice parcial só contém linhas que batem com o `WHERE`. Para filas (outbox, entregas pendentes) a tabela cresce para sempre, mas os pendentes são poucos — o índice parcial fica minúsculo e o claim do worker fica constante.

**Incorreto (índice cheio para uma fila):**

```sql
CREATE INDEX "IX_OutboxEvent_ProcessedAt" ON "OutboxEvent" ("ProcessedAt", "ProximaTentativaEm");
-- carrega milhões de eventos já processados
```

```csharp
builder.HasIndex(x => new { x.ProcessedAt, x.ProximaTentativaEm });
```

**Correto (só o que o worker procura):**

```sql
CREATE INDEX "IX_OutboxEvent_Pendentes"
    ON "OutboxEvent" ("ProcessedAt", "ProximaTentativaEm")
    WHERE "ProcessedAt" IS NULL;
```

```csharp
// OutboxEventConfiguracao.cs (padrão real)
builder.HasIndex(x => new { x.ProcessedAt, x.ProximaTentativaEm })
    .HasFilter("\"ProcessedAt\" IS NULL");
```

A query precisa repetir o predicado **literalmente** para o planner usar o índice: `WHERE "ProcessedAt" IS NULL`. Com enum armazenado como int, o filtro é `"Status" = 0` — se mudar o valor do enum, o índice fica errado silenciosamente.

Índice parcial **único** também implementa regra de negócio: "um dispositivo ativo por token", "um evento por (agendamento, tipo)".

**No TruckFlow:** `OutboxEventConfiguracao.cs`, `NotificacaoEntregaConfiguracao.cs` (`"Status" = 0` e `"Status" = 1 AND "Canal" = 2 AND "ReceiptCheckedAt" IS NULL`), `DispositivoUsuarioConfiguracao.cs` (`"Ativo" = true`), `RecebimentoEventoConfiguracao.cs` (unique `(AgendamentoId, Tipo) WHERE "AgendamentoId" IS NOT NULL`).

Referência: [Partial Indexes](https://www.postgresql.org/docs/16/indexes-partial.html) · [Haki Benita — Indexes you didn't know about](https://hakibenita.com/postgresql-unknown-features)
