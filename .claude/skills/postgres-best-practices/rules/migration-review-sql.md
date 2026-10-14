---
title: Revise o SQL gerado de toda migration antes do merge
impact: HIGH
impactDescription: pega drop/recreate acidental, rewrite de tabela e FK sem índice antes de produção
tags: migrations, review, ef-core, migrations-script
---

## Revise o SQL gerado de toda migration antes do merge

O EF gera migration a partir do diff do model — e às vezes o diff é destrutivo: renomear propriedade vira `DropColumn` + `AddColumn` (perda de dado), mudar tipo vira `ALTER COLUMN TYPE` (rewrite), mudar nome de tabela recria. Ninguém percebe olhando só o C#.

**Incorreto (merge confiando no C# gerado):**

```csharp
// "renomeei a propriedade Placa → PlacaVeiculo"
migrationBuilder.DropColumn(name: "Placa", table: "Agendamento");          // dado perdido
migrationBuilder.AddColumn<string>(name: "PlacaVeiculo", table: "Agendamento", nullable: true);
```

**Correto (gerar e ler o SQL; corrigir à mão):**

```bash
cd src/TruckFlowApi/TruckFlow
dotnet ef migrations add AddTransportadora --project ../TruckFlowApi.Infra --startup-project .
# SQL só da migration nova (de → para)
dotnet ef migrations script <MigrationAnterior> AddTransportadora --project ../TruckFlowApi.Infra --startup-project .
```

Checklist do SQL:
- [ ] Nenhum `DROP COLUMN`/`DROP TABLE` não intencional
- [ ] `RENAME` em vez de drop+add quando for renomear (ou expand/contract)
- [ ] `CREATE INDEX` em tabela quente → `CONCURRENTLY` (`migration-concurrent-index`)
- [ ] `ALTER COLUMN TYPE` só se compatível sem rewrite (`varchar(n)` → maior / `text` ok)
- [ ] Coluna nova em tabela grande: nullable ou `DEFAULT` constante
- [ ] Entidade nova de tenant: `"EmpresaId" uuid NOT NULL` + FK + índice composto
- [ ] `Down()` funciona (ou está documentado que não volta)
- [ ] Testes de integração (Testcontainers rodam `MigrateAsync` real) passam

**No TruckFlow:** `DatabaseFixture` aplica todas as migrations no Postgres 16 do Testcontainers — migration quebrada falha o `dotnet test` (feedback Aurora: "prod migration quebrou com mock").

Referência: [EF Core — Managing migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing) · [EF Core — Generate SQL scripts](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#sql-scripts)
