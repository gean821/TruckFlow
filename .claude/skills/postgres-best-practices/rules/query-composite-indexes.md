---
title: Índice composto com EmpresaId como coluna líder em tabelas de tenant
impact: CRITICAL
impactDescription: 5-50x em queries multi-coluna; índice único serve tenant + filtro
tags: indexes, composite-index, multi-tenant, column-order
---

## Índice composto com EmpresaId como coluna líder em tabelas de tenant

Toda query em entidade `IEmpresaScoped` recebe `WHERE "EmpresaId" = @p` do filtro global. Um índice que **não** começa por `EmpresaId` obriga o Postgres a combinar índices (bitmap AND) ou filtrar depois. Regra de ordem: **igualdade primeiro** (EmpresaId, status, FK), **intervalo/ordenação por último** (datas).

**Incorreto (índices separados, ordem errada):**

```sql
CREATE INDEX "IX_Notificacao_DestinatarioUsuarioId" ON "Notificacao" ("DestinatarioUsuarioId");
CREATE INDEX "IX_Notificacao_CreatedAt" ON "Notificacao" ("CreatedAt");
-- intervalo antes de igualdade: não serve para "EmpresaId" = ? AND "Status" = ?
CREATE INDEX "IX_Agendamento_DataInicio_EmpresaId" ON "Agendamento" ("DataInicio", "EmpresaId");
```

```csharp
builder.HasIndex(x => x.DestinatarioUsuarioId);
builder.HasIndex(x => x.CreatedAt);
```

**Correto (igualdades → intervalo; EmpresaId na frente):**

```sql
CREATE INDEX "IX_Notificacao_EmpresaId_DestinatarioUsuarioId_CreatedAt"
    ON "Notificacao" ("EmpresaId", "DestinatarioUsuarioId", "CreatedAt");
```

```csharp
// NotificacaoConfiguracao.cs (padrão já existente — siga este)
builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId, x.CreatedAt });
```

O índice serve `WHERE "EmpresaId" = ?`, `WHERE "EmpresaId" = ? AND "DestinatarioUsuarioId" = ?` e o `ORDER BY "CreatedAt" DESC` da paginação — **sem sort**. Não serve query que pule `EmpresaId` (ex.: worker cross-tenant) — esses casos têm índice próprio (ver `query-partial-indexes`).

**No TruckFlow:** bons exemplos em `AuditLogConfiguracao.cs` (`EmpresaId, Timestamp` / `EmpresaId, EntityName, EntityId`), `NotificacaoConfiguracao.cs`, `NotaFiscalItemConfiguracao.cs` (`EmpresaId, Status`). `LocalDescargaConfiguracao.cs` tem só `EmpresaId` — ok para tabela pequena.

Referência: [Multicolumn Indexes](https://www.postgresql.org/docs/16/indexes-multicolumn.html) · [Use The Index, Luke — Concatenated Keys](https://use-the-index-luke.com/sql/where-clause/the-equals-operator/concatenated-keys)
