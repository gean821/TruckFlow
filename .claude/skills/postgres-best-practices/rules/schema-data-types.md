---
title: Escolha tipos explícitos — numeric para peso/valor, text com limite de domínio, enum como int estável
impact: HIGH
impactDescription: corretude fiscal, índices menores, sem conversão implícita
tags: data-types, schema, numeric, enum, ef-core
---

## Escolha tipos explícitos — numeric para peso/valor, text com limite de domínio, enum como int estável

Tipo errado vira bug silencioso: `double` em peso de NF-e arredonda, `text` sem limite aceita 10MB num campo de placa, e enum gravado como int muda de significado se alguém reordenar o `enum` C#.

**Incorreto:**

```csharp
public double PesoBruto { get; set; }          // vira double precision: 1234.5600000000001
public string Placa { get; set; }              // text sem limite
public enum StatusAgendamento { Disponivel, Pendente, Agendado } // valores implícitos
```

```sql
"PesoBruto" double precision, "Placa" text
```

**Correto:**

```csharp
public decimal? PesoBruto { get; set; }
// Configuração
builder.Property(x => x.PesoBruto).HasPrecision(15, 4);   // numeric(15,4)
builder.Property(x => x.PlacaVeiculo).HasMaxLength(8);    // varchar(8)
builder.Property(x => x.ChaveAcesso).HasMaxLength(44).IsFixedLength(); // NF-e: 44 dígitos
// Enum com valores explícitos — NUNCA reordene/reaproveite
public enum StatusAgendamento { Disponivel = 0, Pendente = 1, Agendado = 2, EmAndamento = 3, Finalizado = 4, Cancelado = 5, Expirado = 6 }
```

```sql
"PesoBruto" numeric(15,4), "PlacaVeiculo" varchar(8), "ChaveAcesso" char(44)
```

Guia:

| Dado | Tipo PG | EF |
|---|---|---|
| Id | `uuid` | `Guid` |
| Dinheiro, peso, volume | `numeric(p,s)` | `decimal` + `HasPrecision` |
| Instante | `timestamptz` | `DateTime` (Kind UTC) — ver `schema-timestamptz` |
| Data de calendário | `date` | `DateOnly` |
| Texto curto de domínio | `varchar(n)` | `HasMaxLength(n)` |
| Enum | `integer` | enum C# com valores explícitos |
| Config flexível | `jsonb` | ver `schema-jsonb-config` |

Mudar valor de enum usado em índice parcial/exclusion constraint (`"StatusAgendamento" IN (0,1,2,3)`) exige migration que reescreve o predicado.

**No TruckFlow:** `StatusAgendamento` em `Domain/Enums` já tem valores explícitos (siga o padrão); a exclusion constraint e vários `HasFilter` dependem desses ints.

Referência: [Data Types](https://www.postgresql.org/docs/16/datatype.html) · [Npgsql — Type mapping](https://www.npgsql.org/efcore/mapping/translations.html) · [Wiki — Don't Do This](https://wiki.postgresql.org/wiki/Don%27t_Do_This)
