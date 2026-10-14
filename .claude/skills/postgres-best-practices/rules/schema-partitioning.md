---
title: Particione o histórico de posição por mês e cumpra a retenção LGPD com DROP de partição
impact: HIGH
impactDescription: retenção de 90 dias em O(1) (DROP) em vez de DELETE de milhões de linhas + bloat
tags: partitioning, range, retention, lgpd, tracking, no-timescaledb
---

## Particione o histórico de posição por mês e cumpra a retenção LGPD com DROP de partição

`MotoristaPosicaoHistorico` (~14 inserts/s, ~100M linhas em 90 dias) precisa apagar dado velho por obrigação LGPD. `DELETE ... WHERE "CapturadoEm" < now() - '90 days'` gera milhões de tuplas mortas, WAL enorme e VACUUM pesado. Partição por intervalo de tempo transforma retenção em `DROP TABLE` instantâneo. **Sem TimescaleDB** (revisão em `Docs/rastreamento-motorista-backlog.md`: hospedagem no Brasil não pode depender de extensão).

**Incorreto:**

```sql
CREATE TABLE "MotoristaPosicaoHistorico" (... "CapturadoEm" timestamptz NOT NULL);
-- job diário:
DELETE FROM "MotoristaPosicaoHistorico" WHERE "CapturadoEm" < now() - interval '90 days';
```

**Correto (particionamento declarativo por mês):**

```sql
CREATE TABLE "MotoristaPosicaoHistorico" (
    "MotoristaId"   uuid NOT NULL,
    "AgendamentoId" uuid NOT NULL,
    "EmpresaId"     uuid NOT NULL,
    "Latitude"      double precision NOT NULL,
    "Longitude"     double precision NOT NULL,
    "Accuracy"      real NOT NULL,
    "Velocidade"    real NULL,
    "CapturadoEm"   timestamptz NOT NULL,
    PRIMARY KEY ("MotoristaId", "CapturadoEm")          -- PK precisa conter a chave de partição
) PARTITION BY RANGE ("CapturadoEm");

CREATE TABLE "MotoristaPosicaoHistorico_2026_10" PARTITION OF "MotoristaPosicaoHistorico"
    FOR VALUES FROM ('2026-10-01') TO ('2026-11-01');
CREATE INDEX ON "MotoristaPosicaoHistorico" ("EmpresaId", "AgendamentoId", "CapturadoEm"); -- propaga às partições

-- job mensal (worker com SKIP LOCKED/advisory lock): cria o mês+2 e dropa o que passou de 90 dias
ALTER TABLE "MotoristaPosicaoHistorico" DETACH PARTITION "MotoristaPosicaoHistorico_2026_06" CONCURRENTLY;
DROP TABLE "MotoristaPosicaoHistorico_2026_06";
```

```csharp
// EF: mapeie a tabela pai normalmente; DDL de partição vai em migrationBuilder.Sql(...)
builder.ToTable(nameof(MotoristaPosicaoHistorico));
builder.HasKey(x => new { x.MotoristaId, x.CapturadoEm });
// insert do hot path: batch sem tracking (ver data-batch-inserts), não AddRange + SaveChanges
```

Granularidade mensal + retenção 90 dias = apaga "o mês que saiu por inteiro" (dado fica entre 90 e ~120 dias). Se o jurídico exigir exatos 90, use partição diária ou complemente com `DELETE` só na partição mais antiga. Queries sempre com filtro em `"CapturadoEm"` para **partition pruning**. Considere BRIN em `"CapturadoEm"` dentro de cada partição.

**No TruckFlow:** tabela ainda não existe (ADR-0003 + revisão). Direito de exclusão LGPD (`DELETE /v1/motorista/eu/historico-localizacao`) é `DELETE ... WHERE "MotoristaId" = $1` — use a PK (`MotoristaId` líder).

Referência: [Table Partitioning](https://www.postgresql.org/docs/16/ddl-partitioning.html) · [Crunchy Data — Native partitioning / pg_partman](https://www.crunchydata.com/blog/native-partitioning-with-postgres) · [Laurenz Albe — Partitioning pitfalls](https://www.cybertec-postgresql.com/en/partitioning-as-a-query-optimization-strategy/)
