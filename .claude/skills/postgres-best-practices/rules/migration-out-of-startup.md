---
title: Rode migrations em job separado do pipeline, nunca no startup da API
impact: CRITICAL
impactDescription: elimina corrida entre instâncias por __EFMigrationsHistory e boot travado
tags: migrations, deploy, multi-instance, program-cs, railway
---

## Rode migrations em job separado do pipeline, nunca no startup da API

Com 2+ instâncias subindo juntas, todas chamam `Migrate()`: competem pela `__EFMigrationsHistory`, uma pode aplicar DDL enquanto outra já serve tráfego com o modelo novo, e uma migration lenta faz o healthcheck falhar e o orquestrador matar a instância no meio do DDL.

**Incorreto (atual):**

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    scope.ServiceProvider.SeedRolesAsync().GetAwaiter().GetResult();
}
```

**Correto (artefato idempotente aplicado antes do deploy):**

```bash
# CI: gerar script idempotente e revisar no PR
cd src/TruckFlowApi/TruckFlow
dotnet ef migrations script --idempotent --project ../TruckFlowApi.Infra --startup-project . -o migrate.sql

# ou bundle executável (sem SDK no runner de deploy)
dotnet ef migrations bundle --project ../TruckFlowApi.Infra --startup-project . -o efbundle --self-contained
./efbundle --connection "$MIGRATIONS_CONNECTION_STRING"
```

```text
Pipeline: build → test → db-migrate (role truckflow_owner, lock_timeout, retry) → deploy-api
```

O seed de roles vira passo idempotente do mesmo job (ou `HasData`). Enquanto isso não muda, migrations precisam ser **compatíveis com a versão anterior do código** (expand/contract) mesmo assim.

**No TruckFlow:** ADR-0004 item 2 (pré-piloto). Deploy no Railway é automático no push da `master` — o job de migration precisa rodar antes ou como "pre-deploy command" do Railway; o `cd.yml` tem deploy desligado até isso (`INFRA-03`).

Referência: [EF Core — Applying migrations (SQL scripts, bundles)](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying) · [Microsoft — "Apply migrations at runtime" caveats](https://learn.microsoft.com/ef/core/managing-schemas/migrations/applying#apply-migrations-at-runtime)
