---
title: Mudanças de schema em expand/contract — nunca renomear/remover em um passo
impact: CRITICAL
impactDescription: zero erro em mobile já publicado e em instâncias antigas durante rolling deploy
tags: migrations, expand-contract, zero-downtime, not-null, rename, mobile
---

## Mudanças de schema em expand/contract — nunca renomear/remover em um passo

Durante rolling deploy (ADR-0004 item 1) a versão antiga e a nova da API rodam juntas contra o mesmo banco; e o app do motorista publicado na loja pode levar semanas para atualizar. Renomear coluna, trocar tipo ou adicionar `NOT NULL` direto quebra quem ainda está na versão anterior.

**Incorreto (um passo só):**

```csharp
migrationBuilder.RenameColumn(name: "PlacaVeiculo", table: "Agendamento", newName: "Placa");
migrationBuilder.AddColumn<string>(name: "Transportadora", table: "Agendamento", nullable: false, defaultValue: "");
```

```sql
ALTER TABLE "Agendamento" ADD COLUMN "TransportadoraId" uuid NOT NULL REFERENCES "Transportadora"("Id");
-- valida a FK lendo a tabela inteira sob lock; instâncias antigas falham no INSERT
```

**Correto (expand → migrar → contract, em deploys separados):**

```sql
-- Deploy 1 (expand): coluna nullable, FK sem validar
ALTER TABLE "Agendamento" ADD COLUMN "TransportadoraId" uuid NULL;
ALTER TABLE "Agendamento" ADD CONSTRAINT "FK_Agendamento_Transportadora_TransportadoraId"
    FOREIGN KEY ("TransportadoraId") REFERENCES "Transportadora"("Id") NOT VALID;

-- Backfill em lotes (job, não migration), código novo escreve nos dois formatos

-- Deploy 2: validar sem bloquear escrita
ALTER TABLE "Agendamento" VALIDATE CONSTRAINT "FK_Agendamento_Transportadora_TransportadoraId";
ALTER TABLE "Agendamento" ADD CONSTRAINT "CK_Agendamento_TransportadoraId_NotNull"
    CHECK ("TransportadoraId" IS NOT NULL) NOT VALID;
ALTER TABLE "Agendamento" VALIDATE CONSTRAINT "CK_Agendamento_TransportadoraId_NotNull";
-- PG 12+: SET NOT NULL usa o CHECK validado e não varre a tabela
ALTER TABLE "Agendamento" ALTER COLUMN "TransportadoraId" SET NOT NULL;

-- Deploy 3 (contract): só quando nenhum cliente usa o formato antigo
```

```csharp
// Deploy 1 em EF: AddColumn nullable + FK via Sql com NOT VALID
migrationBuilder.AddColumn<Guid>(name: "TransportadoraId", table: "Agendamento", type: "uuid", nullable: true);
migrationBuilder.Sql("""ALTER TABLE "Agendamento" ADD CONSTRAINT "FK_Agendamento_Transportadora_TransportadoraId" FOREIGN KEY ("TransportadoraId") REFERENCES "Transportadora"("Id") NOT VALID;""");
```

Renomear = adicionar nova + escrever nas duas + ler da nova + remover a antiga. Para DTO público (mobile), o contrato muda **depois** do banco, com campo novo opcional.

**No TruckFlow:** o mobile (`C:\tf-mobile`) consome `AgendamentoMotorista/*` e `NotaFiscal/*`; qualquer coluna exposta nesses DTOs segue expand/contract.

Referência: [ALTER TABLE — NOT VALID / VALIDATE](https://www.postgresql.org/docs/16/sql-altertable.html) · [Martin Fowler — Parallel Change](https://martinfowler.com/bliki/ParallelChange.html) · [Strong Migrations (regras seguras)](https://github.com/ankane/strong_migrations)
