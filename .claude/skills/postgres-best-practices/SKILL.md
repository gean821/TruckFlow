---
name: postgres-best-practices
description: Boas práticas de PostgreSQL 16 + EF Core 9 + Npgsql 9 para o TruckFlow. Use ao escrever/revisar queries LINQ ou SQL cru, criar migrations, desenhar schema/índices, tratar concorrência (xmin, exclusion constraint, SKIP LOCKED), multi-tenant (HasQueryFilter por EmpresaId), pooling, LISTEN/NOTIFY, particionamento e diagnóstico de performance.
license: MIT
metadata:
  author: TruckFlow
  version: "1.0.0"
  baseadoEm: supabase/postgres-best-practices (MIT)
---

# PostgreSQL Best Practices — TruckFlow

Guia de performance e corretude para o banco do TruckFlow, priorizado por impacto. Cada regra mostra o anti-padrão e a correção **em SQL e no equivalente EF Core/Npgsql** (LINQ, Fluent API em `TruckFlowApi.Infra/Database/EntitiesMapping/*Configuracao.cs` ou `migrationBuilder.Sql(...)`), com uma nota "No TruckFlow" apontando o código real.

Contexto que pesa em toda decisão (ADR-0001): **multi-tenant fiscal** (21 fábricas Aurora, marcas concorrentes — vazamento entre `Empresa`s é incidente competitivo), 2+ instâncias da API, ~14 inserts/s sustentados no rastreamento, volume modesto. O gargalo não é throughput: é **isolamento, concorrência correta e migration sem downtime**.

## Quando aplicar

- Escrevendo LINQ que vai virar SQL (Include, projeção, paginação, bulk)
- Criando/revisando migration (`dotnet ef migrations add ...`)
- Adicionando entidade nova (é `IEmpresaScoped`? precisa de `xmin`? índice com `EmpresaId` na frente?)
- Worker que consome fila (outbox, dispatcher, expiração)
- Query lenta, lock esperando, pool esgotado
- Qualquer `IgnoreQueryFilters()` ou SQL cru

## Categorias por prioridade

| Prioridade | Categoria | Impacto | Prefixo |
|---|---|---|---|
| 1 | Performance de query | CRITICAL | `query-` |
| 2 | Gestão de conexões | CRITICAL | `conn-` |
| 3 | Segurança & multi-tenant | CRITICAL | `security-` |
| 4 | Migrations seguras | CRITICAL | `migration-` |
| 5 | Design de schema | HIGH | `schema-` |
| 6 | Concorrência & locking | MEDIUM-HIGH | `lock-` |
| 7 | Padrões de acesso a dados | MEDIUM | `data-` |
| 8 | Monitoramento & diagnóstico | LOW-MEDIUM | `monitor-` |
| 9 | Recursos avançados | LOW | `advanced-` |

## Regras que você mais vai precisar

- `security-tenant-query-filter` — toda entidade de tenant implementa `IEmpresaScoped`
- `security-ignore-query-filters` — `IgnoreQueryFilters()` só em métodos `*AcrossTenants`
- `lock-optimistic-xmin` — `xmin` + `DbUpdateConcurrencyException` → `BusinessException`
- `schema-exclusion-constraint` — sem sobreposição de horário por doca (SQLSTATE `23P01`)
- `data-executeupdate-delete` — bulk **pula interceptors** (tenant/audit/outbox)
- `migration-concurrent-index` / `migration-expand-contract` — migration sem travar tabela quente
- `query-composite-indexes` — `EmpresaId` como coluna líder

## Como usar

Leia a regra individual em `rules/<prefixo>-<nome>.md`. Seções e impactos em `rules/_sections.md`. Documento compilado com tudo: `AGENTS.md` (regerar com `python .claude/skills/postgres-best-practices/build_agents.py`).

Agente relacionado: `postgres-pro`. Para o código C# em volta (services, repositórios, exceções): skill `dotnet-best-practices` e agente `dotnet-backend-specialist`.
