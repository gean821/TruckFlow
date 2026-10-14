---
title: Hot path de insert em lote — batch do EF ou binary COPY do Npgsql
impact: MEDIUM
impactDescription: 10-50x mais throughput que insert por linha com SaveChanges
tags: batch-insert, copy, npgsql, bulk, tracking
---

## Hot path de insert em lote — batch do EF ou binary COPY do Npgsql

Inserir pings de posição um a um (`Add` + `SaveChanges` por ping) custa round-trip + transação + tracking por linha. O EF Core já agrupa vários INSERTs num batch por `SaveChanges`; para volume alto e contínuo, `COPY` binário do Npgsql é a via mais rápida.

**Incorreto:**

```csharp
foreach (var p in dto.Posicoes)
{
    _db.MotoristaPosicaoHistorico.Add(Map(p));
    await _db.SaveChangesAsync(ct);          // 1 round-trip + 1 transação por ponto
}
```

**Correto (lote da requisição numa operação):**

```csharp
// Opção A: EF batch (simples; interceptors rodam)
_db.MotoristaPosicaoHistorico.AddRange(dto.Posicoes.Select(Map));
await _db.SaveChangesAsync(ct);              // INSERT multi-row em batches (MaxBatchSize default 42)

// Opção B: COPY binário (hot path ADR-0003; sem tracking, sem interceptors → EmpresaId explícito)
await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
await conn.OpenAsync(ct);
await using var writer = await conn.BeginBinaryImportAsync(
    """COPY "MotoristaPosicaoHistorico" ("MotoristaId","AgendamentoId","EmpresaId","Latitude","Longitude","Accuracy","Velocidade","CapturadoEm") FROM STDIN (FORMAT BINARY)""", ct);
foreach (var p in dto.Posicoes)
{
    await writer.StartRowAsync(ct);
    await writer.WriteAsync(motoristaId, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(dto.AgendamentoId, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(empresaId, NpgsqlDbType.Uuid, ct);      // do contexto, nunca do body
    // ... demais colunas
    await writer.WriteAsync(p.CapturadoEm.ToUniversalTime(), NpgsqlDbType.TimestampTz, ct);
}
await writer.CompleteAsync(ct);
```

```sql
INSERT INTO "MotoristaPosicaoHistorico" (...) VALUES ($1,...), ($9,...), ($17,...);
```

COPY pula o `EmpresaScopedSaveChangesInterceptor` — o service valida tenant/motorista/agendamento antes e escreve `EmpresaId` explicitamente. Combine com upsert da posição atual (`data-upsert`).

**No TruckFlow:** ADR-0003 pede "não usar EF tracking — é hot path" para `POST /v1/motorista/posicao`; ~14 req/s × batch.

Referência: [Npgsql — COPY](https://www.npgsql.org/doc/copy.html) · [EF Core — Batching](https://learn.microsoft.com/ef/core/performance/efficient-updating#batching) · [Populating a Database](https://www.postgresql.org/docs/16/populate.html)
