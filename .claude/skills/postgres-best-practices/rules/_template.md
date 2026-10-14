---
title: Título claro e orientado a ação (ex.: "Use índice parcial para filtros fixos")
impact: MEDIUM
impactDescription: 5-20x mais rápido em queries filtradas
tags: indexes, query-optimization, ef-core
---

## [Título da regra]

[1-3 frases: o problema e por que importa — performance, corretude ou isolamento de tenant.]

**Incorreto (descreva o problema):**

```sql
-- O que torna isso lento/perigoso
CREATE INDEX "IX_Notificacao_DestinatarioUsuarioId" ON "Notificacao" ("DestinatarioUsuarioId");
```

```csharp
// Equivalente EF Core (LINQ / Fluent API / migration)
builder.HasIndex(x => x.DestinatarioUsuarioId);
```

**Correto (descreva a solução):**

```sql
-- Por que é melhor
CREATE INDEX "IX_Notificacao_EmpresaId_DestinatarioUsuarioId"
    ON "Notificacao" ("EmpresaId", "DestinatarioUsuarioId")
    WHERE "LidaEm" IS NULL;
```

```csharp
builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId })
    .HasFilter("\"LidaEm\" IS NULL");
```

**No TruckFlow:** [arquivo real / migration / ADR onde isso aparece ou deveria aparecer]

[Opcional: trade-offs, casos de borda]

Referência: [PostgreSQL Docs](https://www.postgresql.org/docs/16/)
