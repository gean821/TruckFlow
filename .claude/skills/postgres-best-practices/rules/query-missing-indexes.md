---
title: Indexe as colunas de WHERE, JOIN e ORDER BY das queries quentes
impact: CRITICAL
impactDescription: 100-1000x em tabelas grandes (seq scan → index scan)
tags: indexes, performance, sequential-scan, ef-core, foreign-keys
---

## Indexe as colunas de WHERE, JOIN e ORDER BY das queries quentes

Sem índice, o Postgres lê a tabela inteira (seq scan) para cada filtro. Em `"Agendamento"` com 21 fábricas e meses de histórico isso é a diferença entre 2ms e 2s.

**Incorreto (filtro sem índice):**

```sql
-- Tela "agendamentos do dia" — seq scan em toda a tabela
SELECT * FROM "Agendamento"
WHERE "EmpresaId" = @empresaId AND "StatusAgendamento" = 2
  AND "DataInicio" >= @inicio AND "DataInicio" < @fim
ORDER BY "DataInicio";
```

```csharp
// LINQ — o filtro global adiciona "EmpresaId" = @empresaId, mas nada garante índice
var hoje = await _db.Agendamento
    .Where(a => a.StatusAgendamento == StatusAgendamento.Agendado
             && a.DataInicio >= inicio && a.DataInicio < fim)
    .OrderBy(a => a.DataInicio)
    .ToListAsync(ct);
```

**Correto (índice alinhado ao padrão de acesso):**

```sql
CREATE INDEX "IX_Agendamento_EmpresaId_StatusAgendamento_DataInicio"
    ON "Agendamento" ("EmpresaId", "StatusAgendamento", "DataInicio");
```

```csharp
// AgendamentoConfiguracao.cs
builder.HasIndex(x => new { x.EmpresaId, x.StatusAgendamento, x.DataInicio });
```

**Chaves estrangeiras:** o EF Core **já cria índice automaticamente** para cada FK por convenção (`IX_<Tabela>_<Fk>`) — não duplique. Mas esse índice é de coluna única; se toda query filtra por `EmpresaId` + FK, o índice composto (`query-composite-indexes`) é o que o planner vai usar, e o índice simples da FK pode virar peso morto (avalie com `pg_stat_user_indexes.idx_scan = 0`).

Ache candidatos:

```sql
-- Tabelas com muito seq scan
SELECT relname, seq_scan, seq_tup_read, idx_scan
FROM pg_stat_user_tables
WHERE seq_scan > 0 ORDER BY seq_tup_read DESC LIMIT 20;
```

**No TruckFlow:** `AgendamentoConfiguracao.cs` tem `(LocalDescargaId, DataInicio, DataFim)` para checagem de conflito; listagens filtradas por status/data dependem do filtro de tenant — confira o plano com `monitor-explain-analyze` antes de assumir que está coberto.

Referência: [Use The Index, Luke — Markus Winand](https://use-the-index-luke.com/) · [EF Core Indexes](https://learn.microsoft.com/ef/core/modeling/indexes)
