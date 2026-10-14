---
title: Upsert atômico com INSERT ... ON CONFLICT, não SELECT-then-INSERT
impact: MEDIUM
impactDescription: elimina corrida e duplicate key sob concorrência; 1 round-trip
tags: upsert, on-conflict, concurrency, idempotency
---

## Upsert atômico com INSERT ... ON CONFLICT, não SELECT-then-INSERT

"Existe? então UPDATE, senão INSERT" tem janela de corrida: duas requests simultâneas não acham e ambas inserem → `23505 unique_violation` (ou dado duplicado se não há unique). Exemplos reais: registrar dispositivo push (mesmo `ExpoPushToken`), última posição do motorista, mapeamento produto-fornecedor.

**Incorreto:**

```csharp
var disp = await _db.DispositivoUsuario.FirstOrDefaultAsync(d => d.ExpoPushToken == token, ct);
if (disp is null) _db.DispositivoUsuario.Add(new DispositivoUsuario { ... });
else { disp.UsuarioId = userId; disp.UltimoUsoEm = DateTime.UtcNow; disp.Ativo = true; }
await _db.SaveChangesAsync(ct);   // dois cold starts simultâneos → 23505
```

**Correto:**

```sql
INSERT INTO "DispositivoUsuario" ("Id","UsuarioId","ExpoPushToken","Plataforma","AppVersion","UltimoUsoEm","Ativo","CreatedAt")
VALUES ($1,$2,$3,$4,$5,now(),true,now())
ON CONFLICT ("ExpoPushToken") DO UPDATE
SET "UsuarioId" = EXCLUDED."UsuarioId",
    "AppVersion" = EXCLUDED."AppVersion",
    "UltimoUsoEm" = EXCLUDED."UltimoUsoEm",
    "Ativo" = true
RETURNING "Id";
```

```csharp
await _db.Database.ExecuteSqlInterpolatedAsync($"""
    INSERT INTO "MotoristaPosicaoAtual" ("MotoristaId","AgendamentoId","EmpresaId","Latitude","Longitude","CapturadoEm","RecebidoEm")
    VALUES ({motoristaId},{agendamentoId},{empresaId},{lat},{lng},{capturadoEm},now())
    ON CONFLICT ("MotoristaId") DO UPDATE
    SET "Latitude" = EXCLUDED."Latitude", "Longitude" = EXCLUDED."Longitude",
        "CapturadoEm" = EXCLUDED."CapturadoEm", "RecebidoEm" = now()
    WHERE "MotoristaPosicaoAtual"."CapturadoEm" < EXCLUDED."CapturadoEm"   -- ping atrasado não sobrescreve o novo
    """, ct);
```

Alternativa sem SQL cru: tentar insert e capturar `PostgresErrorCodes.UniqueViolation` → recarregar e atualizar. Upsert via SQL cru pula interceptors — preencha `EmpresaId`/`UpdatedAt` explicitamente. `MERGE` (PG 15+) existe, mas `ON CONFLICT` é mais simples e seguro sob concorrência.

**No TruckFlow:** `DispositivoUsuario.ExpoPushToken` unique (`DispositivoUsuarioConfiguracao.cs:50`); `MotoristaPosicaoAtual` (ADR-0003: "upsert atômico, raw SQL").

Referência: [INSERT ... ON CONFLICT](https://www.postgresql.org/docs/16/sql-insert.html#SQL-ON-CONFLICT) · [Npgsql — Error codes](https://www.npgsql.org/doc/api/Npgsql.PostgresErrorCodes.html)
