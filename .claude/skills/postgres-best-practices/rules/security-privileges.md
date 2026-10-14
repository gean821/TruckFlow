---
title: Role da aplicação com privilégio mínimo; migrations com outro role
impact: HIGH
impactDescription: limita o estrago de SQL injection ou credencial vazada
tags: privileges, roles, least-privilege, migrations, security
---

## Role da aplicação com privilégio mínimo; migrations com outro role

Se a API conecta como superusuário/dono do schema, uma credencial vazada ou um SQL injection pode `DROP TABLE`, ler `pg_shadow` ou desligar RLS. A aplicação precisa só de DML.

**Incorreto (um usuário para tudo):**

```text
ConnectionStrings__DefaultConnection=Host=db;Username=postgres;Password=...
```

**Correto (separação de papéis):**

```sql
-- dono do schema: usado SÓ pelo job de migration
CREATE ROLE truckflow_owner LOGIN PASSWORD '...';
-- aplicação: DML apenas
CREATE ROLE truckflow_app LOGIN PASSWORD '...';
GRANT USAGE ON SCHEMA public TO truckflow_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO truckflow_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO truckflow_app;
ALTER DEFAULT PRIVILEGES FOR ROLE truckflow_owner IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO truckflow_app;
ALTER DEFAULT PRIVILEGES FOR ROLE truckflow_owner IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO truckflow_app;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
```

```bash
# job de migration (pipeline), não o Program.cs
dotnet ef database update --connection "Host=db;Username=truckflow_owner;..."
```

Pré-requisito: tirar `db.Database.Migrate()` do startup (ADR-0004 item 2) — enquanto a API migra no boot, ela precisa de DDL.

**No TruckFlow:** hoje `Program.cs` roda `Migrate()` no startup com a mesma connection string. Segredos: user-secrets em dev, env vars em prod (`ConnectionStrings__DefaultConnection`); nunca em `appsettings.json` versionado (ADR-0004 item 5).

Referência: [Privileges](https://www.postgresql.org/docs/16/ddl-priv.html) · [ALTER DEFAULT PRIVILEGES](https://www.postgresql.org/docs/16/sql-alterdefaultprivileges.html) · [Crunchy Data — Postgres security checklist](https://www.crunchydata.com/blog/postgres-security-checklist)
