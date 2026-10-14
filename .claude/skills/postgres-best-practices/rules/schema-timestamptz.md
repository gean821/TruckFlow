---
title: Instantes em timestamptz com DateTime UTC; fuso de São Paulo só nas bordas
impact: HIGH
impactDescription: elimina erro de 3h em janela de agendamento e exceções do Npgsql 6+
tags: timestamptz, datetime, utc, timezone, npgsql
---

## Instantes em timestamptz com DateTime UTC; fuso de São Paulo só nas bordas

Desde o Npgsql 6, `DateTime` mapeia para `timestamptz` e o Npgsql **exige `Kind=Utc`** ao gravar (lança `ArgumentException` com `Kind=Local`/`Unspecified`). O banco armazena o instante; o fuso é apresentação. Janela de descarga "14:00" é horário de São Paulo — converta na entrada (DTO → UTC) e na saída (UTC → DTO), nunca no meio.

**Incorreto:**

```csharp
var inicio = DateTime.Now;                                   // Local: Npgsql lança / grava errado
var vencidos = _db.Agendamento.Where(a => a.DataFim < DateTime.Now);
// validator avaliado no construtor e em hora local:
RuleFor(x => x.DataInicio).GreaterThan(DateTime.Now.AddMinutes(-5));
// liga o modo legado para "resolver":
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

**Correto:**

```csharp
var agora = DateTime.UtcNow;
var vencidos = _db.Agendamento.Where(a => a.DataFim < agora);
// validator com lambda (avaliada a cada validação) e em UTC:
RuleFor(x => x.DataInicio).Must(d => d.ToUniversalTime() > DateTime.UtcNow.AddMinutes(-5));
// borda: "dia operacional" em São Paulo → intervalo UTC
var tz = Grade.OperationalTimeZone; // America/Sao_Paulo
var inicioDiaUtc = TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(TimeOnly.MinValue), tz);
var fimDiaUtc = inicioDiaUtc.AddDays(1);
```

```sql
-- "agendamentos de hoje" no fuso da fábrica, sem converter a coluna (mantém o índice)
SELECT * FROM "Agendamento"
WHERE "EmpresaId" = $1
  AND "DataInicio" >= ($2::date::timestamp AT TIME ZONE 'America/Sao_Paulo')
  AND "DataInicio" <  (($2::date + 1)::timestamp AT TIME ZONE 'America/Sao_Paulo');
```

Nunca aplique função na coluna do `WHERE` (`date("DataInicio" AT TIME ZONE ...) = $2`) — mata o índice. Data de calendário pura (sem hora) é `DateOnly` → `date`.

**No TruckFlow:** `Grade.OperationalTimeZone` é o fuso operacional compartilhado; `AgendamentoAdminCreateValidator` usa `DateTime.Now` (dívida conhecida — não copie). Se as 21 fábricas cruzarem fusos (MT/MS), o fuso vira configuração por `Empresa` (`Empresa.Configuracoes`).

Referência: [Npgsql 6 — timestamp breaking changes](https://www.npgsql.org/doc/types/datetime.html) · [Date/Time Types](https://www.postgresql.org/docs/16/datatype-datetime.html) · [Wiki — Don't use timestamp without time zone](https://wiki.postgresql.org/wiki/Don%27t_Do_This#Don.27t_use_timestamp_.28without_time_zone.29)
