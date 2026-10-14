---
title: Identificadores são PascalCase entre aspas — mantenha a convenção e sempre cite no SQL cru
impact: MEDIUM
impactDescription: evita "relation does not exist" e mistura de convenções
tags: identifiers, naming, quoting, schema, ef-core
---

## Identificadores são PascalCase entre aspas — mantenha a convenção e sempre cite no SQL cru

O TruckFlow mapeia `ToTable(nameof(Entity))` com colunas PascalCase (`"Agendamento"."EmpresaId"`). No Postgres, identificador sem aspas vira minúsculo — `SELECT * FROM Agendamento` procura `agendamento` e falha. A recomendação clássica do Postgres é snake_case sem aspas; **mas trocar agora custa uma migration de todas as tabelas sem ganho funcional**. Consistência vence.

**Incorreto:**

```sql
SELECT * FROM Agendamento WHERE EmpresaId = $1;           -- relation "agendamento" does not exist
CREATE TABLE motorista_posicao_atual (...);                -- convenção nova misturada
```

```csharp
builder.ToTable("motorista_posicao_atual");                // quebra o padrão
_db.Database.ExecuteSqlRaw("UPDATE Notificacao SET ...");  // sem aspas + raw
```

**Correto:**

```sql
SELECT * FROM "Agendamento" WHERE "EmpresaId" = $1;
```

```csharp
builder.ToTable(nameof(MotoristaPosicaoAtual));
await _db.Database.ExecuteSqlInterpolatedAsync(
    $"""UPDATE "Notificacao" SET "LidaEm" = now() WHERE "Id" = {id}""", ct);
```

Em C#, use raw string literal (`"""..."""`) para SQL com aspas duplas — evita `\"` escapado. Nomes de índice/constraint também seguem o padrão EF (`IX_Tabela_Coluna`, `FK_...`, `PK_...`); constraints manuais podem usar snake_case descritivo (`agendamento_no_overlap_per_doca` já existe — ok).

**No TruckFlow:** padrão em todas as `*Configuracao.cs`; SQL cru em `NotificacaoEntregaRepositorio.cs` e `OutboxProcessorWorker` já cita corretamente. Se um dia quiser snake_case, use `EFCore.NamingConventions` numa migration dedicada — decisão de ADR, não de PR.

Referência: [Identifiers and Key Words](https://www.postgresql.org/docs/16/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS) · [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions)
