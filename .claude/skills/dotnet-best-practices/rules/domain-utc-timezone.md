---
title: UTC em tudo; America/Sao_Paulo só na borda via Grade.OperationalTimeZone
impact: HIGH
impactDescription: janelas de descarga não "andam" 3h entre dev, Railway e Azure
tags: datetime, utc, timezone, npgsql, timestamptz
---

## UTC em tudo; America/Sao_Paulo só na borda via Grade.OperationalTimeZone

Npgsql 6+ mapeia `DateTime` com `Kind=Utc` para `timestamptz` e **rejeita** `Kind=Local/Unspecified` nesse tipo — o que é bom: força a disciplina. Toda data gravada e comparada é UTC (`DateTime.UtcNow`). Conversão para o horário operacional da fábrica (`Grade.OperationalTimeZone` = America/Sao_Paulo) acontece apenas onde a regra é "de calendário": gerar slots do dia, "agendamentos de hoje", relatório diário, exibição. Front e mobile recebem ISO-8601 com `Z` e formatam localmente.

**Incorreto:**

```csharp
var hoje = DateTime.Today;                                   // fuso do servidor
var doDia = _db.Agendamento.Where(a => a.DataInicio.Date == hoje);  // "dia" em UTC ≠ dia na fábrica
```

**Correto:**

```csharp
var tz = Grade.OperationalTimeZone;
var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
var inicioLocal = agoraLocal.Date;
var inicioUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz);
var fimUtc = TimeZoneInfo.ConvertTimeToUtc(inicioLocal.AddDays(1), tz);

var doDia = _db.Agendamento.Where(a => a.DataInicio >= inicioUtc && a.DataInicio < fimUtc); // sargável
```

Intervalos sempre semiabertos `[início, fim)` — igual à exclusion constraint (`tstzrange(..., '[)')`). `DateOnly` em `XListQueryDto` (filtro de tela) é dia **operacional**: converta para o intervalo UTC no service. Se um dia houver fábrica fora de BRT, o fuso vira configuração da `Empresa` (`Empresa.Configuracoes`, ADR-0001) — não espalhe `"America/Sao_Paulo"` literal.

**No TruckFlow:** `Grade.OperationalTimeZone`; dívida de `DateTime.Now` em `AgendamentoAdminCreateValidator` (ver `service-validator-clock`).

Referência: [Npgsql — Date and Time Handling](https://www.npgsql.org/doc/types/datetime.html) · [PostgreSQL — Don't use timestamp without time zone](https://wiki.postgresql.org/wiki/Don%27t_Do_This#Don.27t_use_timestamp_.28without_time_zone.29) · [Jon Skeet — NodaTime / date-time pitfalls](https://nodatime.org/)
