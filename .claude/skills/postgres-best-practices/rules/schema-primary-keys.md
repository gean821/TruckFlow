---
title: Chaves primárias Guid — prefira UUIDv7 (ordenável) para tabelas de alto insert
impact: MEDIUM
impactDescription: menos fragmentação e WAL em índices de PK de tabelas grandes
tags: primary-keys, uuid, uuidv7, guid, schema
---

## Chaves primárias Guid — prefira UUIDv7 (ordenável) para tabelas de alto insert

O TruckFlow usa `Guid` gerado no cliente (`EntidadeBase.Id = Guid.NewGuid()`) — bom para multi-instância e para criar agregados com FK antes do `SaveChanges`. Mas `Guid.NewGuid()` é **v4 aleatório**: cada insert cai numa página aleatória do índice da PK, causando page splits e mais WAL. Irrelevante em cadastros; relevante em tabelas de ~14 inserts/s contínuos (rastreamento, outbox, audit).

**Incorreto (v4 em tabela append-heavy):**

```csharp
public Guid Id { get; set; } = Guid.NewGuid(); // aleatório
```

```sql
-- índice PK com inserts espalhados → bloat e cache miss
```

**Correto (UUIDv7: prefixo temporal, ordenado):**

```csharp
// .NET 9+: Guid.CreateVersion7(); no .NET 8 use a lib UUIDNext ou gere no banco
public Guid Id { get; set; } = UUIDNext.Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
```

```sql
-- PG 18 terá uuidv7() nativo; até lá, gerar no cliente
```

Não troque a PK de tabelas existentes por bigint/identity: o custo de migrar FKs e contratos (front/mobile usam `id: string` uuid) não compensa. Para tabelas **sem** identidade de negócio e enormes (histórico de posição), a PK pode ser composta `(MotoristaId, CapturadoEm)` — e precisa incluir a chave de partição (`schema-partitioning`).

**No TruckFlow:** `TruckFlow.Domain/Entities/EntidadeBase.cs`. Candidatos a v7: `OutboxEvent`, `AuditLog`, `NotificacaoEntrega`, futura `MotoristaPosicaoHistorico`.

Referência: [RFC 9562 — UUIDv7](https://www.rfc-editor.org/rfc/rfc9562) · [Brandur — Identity crisis: sequence vs UUID](https://brandur.org/nanoglyphs/026-ids) · [Cybertec — UUID performance](https://www.cybertec-postgresql.com/en/unexpected-downsides-of-uuid-keys-in-postgresql/)
