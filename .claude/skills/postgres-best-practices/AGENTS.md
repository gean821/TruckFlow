# PostgreSQL Best Practices — TruckFlow

**Versão 1.0.0**  
TruckFlow  
Outubro 2026

> Documento gerado por `build_agents.py` a partir de `rules/*.md`. Não edite à mão.

---

## Resumo

Guia de boas práticas de PostgreSQL 16 com EF Core 9 e Npgsql 9 para o backend multi-tenant do TruckFlow (cliente Aurora Alimentos, 21 fábricas). Regras em 9 categorias priorizadas por impacto, cada uma com anti-padrão e correção em SQL e em EF Core/Npgsql, notas apontando o código real e referências. Adaptado de supabase/postgres-best-practices (MIT), substituindo RLS/Supabase por multi-tenant via global query filter e adicionando migrations seguras, concorrência (xmin, exclusion constraint, SKIP LOCKED), idempotência e LISTEN/NOTIFY.

---

## Sumário

1. [Performance de query](#performance-de-query) - **CRITICAL**
   - 1.1 [Índice composto com EmpresaId como coluna líder em tabelas de tenant](#11-índice-composto-com-empresaid-como-coluna-líder-em-tabelas-de-tenant)
   - 1.2 [Indexe as colunas de WHERE, JOIN e ORDER BY das queries quentes](#12-indexe-as-colunas-de-where-join-e-order-by-das-queries-quentes)
   - 1.3 [Escolha o tipo de índice certo (B-tree, GIN, GiST, BRIN)](#13-escolha-o-tipo-de-índice-certo-b-tree-gin-gist-brin)
   - 1.4 [Use índice parcial para filtros fixos (filas, pendentes, ativos)](#14-use-índice-parcial-para-filtros-fixos-filas-pendentes-ativos)
   - 1.5 [Use covering index (INCLUDE) para listagens e contagens quentes](#15-use-covering-index-include-para-listagens-e-contagens-quentes)
2. [Gestão de conexões](#gestão-de-conexões) - **CRITICAL**
   - 2.1 [Calcule o orçamento de conexões por componente](#21-calcule-o-orçamento-de-conexões-por-componente)
   - 2.2 [Dimensione o pool do Npgsql e planeje o PgBouncer com os caveats do modo transaction](#22-dimensione-o-pool-do-npgsql-e-planeje-o-pgbouncer-com-os-caveats-do-modo-transaction)
   - 2.3 [Configure timeouts para transação ociosa e statements longos](#23-configure-timeouts-para-transação-ociosa-e-statements-longos)
   - 2.4 [Entenda prepared statements do Npgsql antes de ligar PgBouncer](#24-entenda-prepared-statements-do-npgsql-antes-de-ligar-pgbouncer)
3. [Segurança & multi-tenant](#segurança-multi-tenant) - **CRITICAL**
   - 3.1 [IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant](#31-ignorequeryfilters-só-em-métodos-acrosstenants-seguidos-de-withtenant)
   - 3.2 [Toda entidade de tenant implementa IEmpresaScoped — o filtro global falha fechado](#32-toda-entidade-de-tenant-implementa-iempresascoped-o-filtro-global-falha-fechado)
   - 3.3 [Role da aplicação com privilégio mínimo; migrations com outro role](#33-role-da-aplicação-com-privilégio-mínimo-migrations-com-outro-role)
   - 3.4 [RLS como defesa em profundidade (opcional) — com o caveat de pooling](#34-rls-como-defesa-em-profundidade-opcional-com-o-caveat-de-pooling)
4. [Migrations seguras](#migrations-seguras) - **CRITICAL**
   - 4.1 [Crie índice em tabela quente com CONCURRENTLY (fora da transação da migration)](#41-crie-índice-em-tabela-quente-com-concurrently-fora-da-transação-da-migration)
   - 4.2 [Mudanças de schema em expand/contract — nunca renomear/remover em um passo](#42-mudanças-de-schema-em-expandcontract-nunca-renomearremover-em-um-passo)
   - 4.3 [Use lock_timeout em migrations para não enfileirar o tráfego atrás de um ALTER](#43-use-lock_timeout-em-migrations-para-não-enfileirar-o-tráfego-atrás-de-um-alter)
   - 4.4 [Rode migrations em job separado do pipeline, nunca no startup da API](#44-rode-migrations-em-job-separado-do-pipeline-nunca-no-startup-da-api)
   - 4.5 [Revise o SQL gerado de toda migration antes do merge](#45-revise-o-sql-gerado-de-toda-migration-antes-do-merge)
5. [Design de schema](#design-de-schema) - **HIGH**
   - 5.1 [Garanta "sem sobreposição de horário por doca" com exclusion constraint, não com SELECT antes do INSERT](#51-garanta-sem-sobreposição-de-horário-por-doca-com-exclusion-constraint-não-com-select-antes-do-insert)
   - 5.2 [Escolha tipos explícitos — numeric para peso/valor, text com limite de domínio, enum como int estável](#52-escolha-tipos-explícitos-numeric-para-pesovalor-text-com-limite-de-domínio-enum-como-int-estável)
   - 5.3 [FKs têm índice (o EF cria) — mas verifique FKs criadas por SQL cru e índices redundantes](#53-fks-têm-índice-o-ef-cria-mas-verifique-fks-criadas-por-sql-cru-e-índices-redundantes)
   - 5.4 [Particione o histórico de posição por mês e cumpra a retenção LGPD com DROP de partição](#54-particione-o-histórico-de-posição-por-mês-e-cumpra-a-retenção-lgpd-com-drop-de-partição)
   - 5.5 [Instantes em timestamptz com DateTime UTC; fuso de São Paulo só nas bordas](#55-instantes-em-timestamptz-com-datetime-utc-fuso-de-são-paulo-só-nas-bordas)
   - 5.6 [Variação por fábrica em Empresa.Configuracoes (jsonb) — tipada no C#, nunca if por tenant](#56-variação-por-fábrica-em-empresaconfiguracoes-jsonb-tipada-no-c-nunca-if-por-tenant)
   - 5.7 [Chaves primárias Guid — prefira UUIDv7 (ordenável) para tabelas de alto insert](#57-chaves-primárias-guid-prefira-uuidv7-ordenável-para-tabelas-de-alto-insert)
   - 5.8 [Identificadores são PascalCase entre aspas — mantenha a convenção e sempre cite no SQL cru](#58-identificadores-são-pascalcase-entre-aspas-mantenha-a-convenção-e-sempre-cite-no-sql-cru)
6. [Concorrência & locking](#concorrência-locking) - **MEDIUM-HIGH**
   - 6.1 [Concorrência otimista com xmin; DbUpdateConcurrencyException vira BusinessException](#61-concorrência-otimista-com-xmin-dbupdateconcurrencyexception-vira-businessexception)
   - 6.2 [Workers em multi-instância reivindicam trabalho com FOR UPDATE SKIP LOCKED](#62-workers-em-multi-instância-reivindicam-trabalho-com-for-update-skip-locked)
   - 6.3 [Transações curtas — nada de I/O externo (SEFAZ, Expo, e-mail) dentro de transação](#63-transações-curtas-nada-de-io-externo-sefaz-expo-e-mail-dentro-de-transação)
   - 6.4 [Previna deadlock travando linhas em ordem consistente](#64-previna-deadlock-travando-linhas-em-ordem-consistente)
   - 6.5 [Advisory lock (por transação) para jobs singleton entre instâncias](#65-advisory-lock-por-transação-para-jobs-singleton-entre-instâncias)
7. [Padrões de acesso a dados](#padrões-de-acesso-a-dados) - **MEDIUM**
   - 7.1 [ExecuteUpdate/ExecuteDelete são rápidos — e pulam interceptors (tenant, audit, outbox) e o xmin](#71-executeupdateexecutedelete-são-rápidos-e-pulam-interceptors-tenant-audit-outbox-e-o-xmin)
   - 7.2 [Idempotency-Key em POST críticos (reservar, posição) — padrão Stripe com unique no banco](#72-idempotency-key-em-post-críticos-reservar-posição-padrão-stripe-com-unique-no-banco)
   - 7.3 [Elimine N+1 — projeção com Select para leitura, Include só quando vai mutar, AsSplitQuery para várias coleções](#73-elimine-n1-projeção-com-select-para-leitura-include-só-quando-vai-mutar-assplitquery-para-várias-coleções)
   - 7.4 [Paginação offset (PagedResponse) para telas; keyset para feeds/exports grandes](#74-paginação-offset-pagedresponse-para-telas-keyset-para-feedsexports-grandes)
   - 7.5 [Leitura que não vai salvar usa AsNoTracking (ou projeção)](#75-leitura-que-não-vai-salvar-usa-asnotracking-ou-projeção)
   - 7.6 [Hot path de insert em lote — batch do EF ou binary COPY do Npgsql](#76-hot-path-de-insert-em-lote-batch-do-ef-ou-binary-copy-do-npgsql)
   - 7.7 [Upsert atômico com INSERT ... ON CONFLICT, não SELECT-then-INSERT](#77-upsert-atômico-com-insert-on-conflict-não-select-then-insert)
8. [Monitoramento & diagnóstico](#monitoramento-diagnóstico) - **LOW-MEDIUM**
   - 8.1 [Diagnostique com ToQueryString/LogTo + EXPLAIN (ANALYZE, BUFFERS) — não adivinhe](#81-diagnostique-com-toquerystringlogto-explain-analyze-buffers-não-adivinhe)
   - 8.2 [Use pg_stat_statements para achar as queries que realmente custam](#82-use-pg_stat_statements-para-achar-as-queries-que-realmente-custam)
   - 8.3 [Ajuste autovacuum nas tabelas de fila e alto churn](#83-ajuste-autovacuum-nas-tabelas-de-fila-e-alto-churn)
9. [Recursos avançados](#recursos-avançados) - **LOW**
   - 9.1 [LISTEN/NOTIFY para fan-out do SSE — payload pequeno, conexão dedicada, fora do PgBouncer](#91-listennotify-para-fan-out-do-sse-payload-pequeno-conexão-dedicada-fora-do-pgbouncer)
   - 9.2 [Busca textual em português com tsvector + unaccent (ou trigram para nomes curtos)](#92-busca-textual-em-português-com-tsvector-unaccent-ou-trigram-para-nomes-curtos)
   - 9.3 [Indexe jsonb só pelo que é consultado — GIN jsonb_path_ops ou índice de expressão](#93-indexe-jsonb-só-pelo-que-é-consultado-gin-jsonb_path_ops-ou-índice-de-expressão)

---

## Performance de query

**Impacto:** CRITICAL  
Índice faltando ou errado, planos ruins. A causa nº 1 de lentidão — e no TruckFlow quase toda query é filtrada por `EmpresaId` antes de qualquer outra coisa.

### 1.1 Índice composto com EmpresaId como coluna líder em tabelas de tenant

**Impacto: CRITICAL (5-50x em queries multi-coluna; índice único serve tenant + filtro)**  
Tags: indexes, composite-index, multi-tenant, column-order  
Arquivo: `rules/query-composite-indexes.md`

Toda query em entidade `IEmpresaScoped` recebe `WHERE "EmpresaId" = @p` do filtro global. Um índice que **não** começa por `EmpresaId` obriga o Postgres a combinar índices (bitmap AND) ou filtrar depois. Regra de ordem: **igualdade primeiro** (EmpresaId, status, FK), **intervalo/ordenação por último** (datas).

**Incorreto (índices separados, ordem errada):**

```sql
CREATE INDEX "IX_Notificacao_DestinatarioUsuarioId" ON "Notificacao" ("DestinatarioUsuarioId");
CREATE INDEX "IX_Notificacao_CreatedAt" ON "Notificacao" ("CreatedAt");
-- intervalo antes de igualdade: não serve para "EmpresaId" = ? AND "Status" = ?
CREATE INDEX "IX_Agendamento_DataInicio_EmpresaId" ON "Agendamento" ("DataInicio", "EmpresaId");
```

```csharp
builder.HasIndex(x => x.DestinatarioUsuarioId);
builder.HasIndex(x => x.CreatedAt);
```

**Correto (igualdades → intervalo; EmpresaId na frente):**

```sql
CREATE INDEX "IX_Notificacao_EmpresaId_DestinatarioUsuarioId_CreatedAt"
    ON "Notificacao" ("EmpresaId", "DestinatarioUsuarioId", "CreatedAt");
```

```csharp
// NotificacaoConfiguracao.cs (padrão já existente — siga este)
builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId, x.CreatedAt });
```

O índice serve `WHERE "EmpresaId" = ?`, `WHERE "EmpresaId" = ? AND "DestinatarioUsuarioId" = ?` e o `ORDER BY "CreatedAt" DESC` da paginação — **sem sort**. Não serve query que pule `EmpresaId` (ex.: worker cross-tenant) — esses casos têm índice próprio (ver `query-partial-indexes`).

**No TruckFlow:** bons exemplos em `AuditLogConfiguracao.cs` (`EmpresaId, Timestamp` / `EmpresaId, EntityName, EntityId`), `NotificacaoConfiguracao.cs`, `NotaFiscalItemConfiguracao.cs` (`EmpresaId, Status`). `LocalDescargaConfiguracao.cs` tem só `EmpresaId` — ok para tabela pequena.

Referência: [Multicolumn Indexes](https://www.postgresql.org/docs/16/indexes-multicolumn.html) · [Use The Index, Luke — Concatenated Keys](https://use-the-index-luke.com/sql/where-clause/the-equals-operator/concatenated-keys)

---

### 1.2 Indexe as colunas de WHERE, JOIN e ORDER BY das queries quentes

**Impacto: CRITICAL (100-1000x em tabelas grandes (seq scan → index scan))**  
Tags: indexes, performance, sequential-scan, ef-core, foreign-keys  
Arquivo: `rules/query-missing-indexes.md`

Sem índice, o Postgres lê a tabela inteira (seq scan) para cada filtro. Em `"Agendamento"` com 21 fábricas e meses de histórico isso é a diferença entre 2ms e 2s.

**Incorreto (filtro sem índice):**

```sql
-- Tela "agendamentos do dia" — seq scan em toda a tabela
SELECT * FROM "Agendamento"
WHERE "EmpresaId" = @empresaId AND "StatusAgendamento" = 2
  AND "DataInicio" >= @inicio AND "DataInicio" < @fim
ORDER BY "DataInicio";
```

```csharp
// LINQ — o filtro global adiciona "EmpresaId" = @empresaId, mas nada garante índice
var hoje = await _db.Agendamento
    .Where(a => a.StatusAgendamento == StatusAgendamento.Agendado
             && a.DataInicio >= inicio && a.DataInicio < fim)
    .OrderBy(a => a.DataInicio)
    .ToListAsync(ct);
```

**Correto (índice alinhado ao padrão de acesso):**

```sql
CREATE INDEX "IX_Agendamento_EmpresaId_StatusAgendamento_DataInicio"
    ON "Agendamento" ("EmpresaId", "StatusAgendamento", "DataInicio");
```

```csharp
// AgendamentoConfiguracao.cs
builder.HasIndex(x => new { x.EmpresaId, x.StatusAgendamento, x.DataInicio });
```

**Chaves estrangeiras:** o EF Core **já cria índice automaticamente** para cada FK por convenção (`IX_<Tabela>_<Fk>`) — não duplique. Mas esse índice é de coluna única; se toda query filtra por `EmpresaId` + FK, o índice composto (`query-composite-indexes`) é o que o planner vai usar, e o índice simples da FK pode virar peso morto (avalie com `pg_stat_user_indexes.idx_scan = 0`).

Ache candidatos:

```sql
-- Tabelas com muito seq scan
SELECT relname, seq_scan, seq_tup_read, idx_scan
FROM pg_stat_user_tables
WHERE seq_scan > 0 ORDER BY seq_tup_read DESC LIMIT 20;
```

**No TruckFlow:** `AgendamentoConfiguracao.cs` tem `(LocalDescargaId, DataInicio, DataFim)` para checagem de conflito; listagens filtradas por status/data dependem do filtro de tenant — confira o plano com `monitor-explain-analyze` antes de assumir que está coberto.

Referência: [Use The Index, Luke — Markus Winand](https://use-the-index-luke.com/) · [EF Core Indexes](https://learn.microsoft.com/ef/core/modeling/indexes)

---

### 1.3 Escolha o tipo de índice certo (B-tree, GIN, GiST, BRIN)

**Impacto: HIGH (10-100x com o tipo adequado ao operador)**  
Tags: indexes, btree, gin, gist, brin, trigram  
Arquivo: `rules/query-index-types.md`

B-tree (default) serve `=`, `<`, `>`, `BETWEEN`, `ORDER BY` e `LIKE 'abc%'`. Não serve `ILIKE '%abc%'`, operadores de `jsonb` (`@>`, `?`) nem sobreposição de intervalo (`&&`).

**Incorreto (B-tree para busca "contém"):**

```sql
CREATE INDEX "IX_Fornecedor_Nome" ON "Fornecedor" ("Nome");
SELECT * FROM "Fornecedor" WHERE "EmpresaId" = @e AND "Nome" ILIKE '%alimentos%'; -- seq scan
```

```csharp
.Where(f => EF.Functions.ILike(f.Nome, $"%{search}%"))
```

**Correto (trigram GIN para busca parcial):**

```sql
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX "IX_Fornecedor_Nome_trgm" ON "Fornecedor" USING gin ("Nome" gin_trgm_ops);
```

```csharp
// Configuração Npgsql
builder.HasIndex(x => x.Nome).HasMethod("gin").HasOperators("gin_trgm_ops");
// e na migration: migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
```

Guia rápido:

| Tipo | Use para | No TruckFlow |
|---|---|---|
| B-tree | igualdade, intervalo, ordenação | quase tudo |
| GIN | `jsonb` (`@>`), arrays, trigram (`ILIKE '%x%'`), full-text | `Empresa.Configuracoes`, busca de fornecedor/produto |
| GiST | intervalos (`tstzrange &&`), exclusion constraint | `agendamento_no_overlap_per_doca` |
| BRIN | colunas de tempo em tabela append-only enorme | candidato para `MotoristaPosicaoHistorico."CapturadoEm"` |

**No TruckFlow:** a busca `search` das listagens paginadas (`*ListQueryDto.Search`) usa `ILIKE`/`Contains`; com milhares de linhas por tenant, avalie trigram antes de reclamar do debounce do front.

Referência: [Index Types](https://www.postgresql.org/docs/16/indexes-types.html) · [pg_trgm](https://www.postgresql.org/docs/16/pgtrgm.html) · [Npgsql — Index method](https://www.npgsql.org/efcore/modeling/indexes.html)

---

### 1.4 Use índice parcial para filtros fixos (filas, pendentes, ativos)

**Impacto: HIGH (índices 5-20x menores; filas de worker em O(pendentes) e não O(histórico))**  
Tags: indexes, partial-index, queue, workers, hasfilter  
Arquivo: `rules/query-partial-indexes.md`

Índice parcial só contém linhas que batem com o `WHERE`. Para filas (outbox, entregas pendentes) a tabela cresce para sempre, mas os pendentes são poucos — o índice parcial fica minúsculo e o claim do worker fica constante.

**Incorreto (índice cheio para uma fila):**

```sql
CREATE INDEX "IX_OutboxEvent_ProcessedAt" ON "OutboxEvent" ("ProcessedAt", "ProximaTentativaEm");
-- carrega milhões de eventos já processados
```

```csharp
builder.HasIndex(x => new { x.ProcessedAt, x.ProximaTentativaEm });
```

**Correto (só o que o worker procura):**

```sql
CREATE INDEX "IX_OutboxEvent_Pendentes"
    ON "OutboxEvent" ("ProcessedAt", "ProximaTentativaEm")
    WHERE "ProcessedAt" IS NULL;
```

```csharp
// OutboxEventConfiguracao.cs (padrão real)
builder.HasIndex(x => new { x.ProcessedAt, x.ProximaTentativaEm })
    .HasFilter("\"ProcessedAt\" IS NULL");
```

A query precisa repetir o predicado **literalmente** para o planner usar o índice: `WHERE "ProcessedAt" IS NULL`. Com enum armazenado como int, o filtro é `"Status" = 0` — se mudar o valor do enum, o índice fica errado silenciosamente.

Índice parcial **único** também implementa regra de negócio: "um dispositivo ativo por token", "um evento por (agendamento, tipo)".

**No TruckFlow:** `OutboxEventConfiguracao.cs`, `NotificacaoEntregaConfiguracao.cs` (`"Status" = 0` e `"Status" = 1 AND "Canal" = 2 AND "ReceiptCheckedAt" IS NULL`), `DispositivoUsuarioConfiguracao.cs` (`"Ativo" = true`), `RecebimentoEventoConfiguracao.cs` (unique `(AgendamentoId, Tipo) WHERE "AgendamentoId" IS NOT NULL`).

Referência: [Partial Indexes](https://www.postgresql.org/docs/16/indexes-partial.html) · [Haki Benita — Indexes you didn't know about](https://hakibenita.com/postgresql-unknown-features)

---

### 1.5 Use covering index (INCLUDE) para listagens e contagens quentes

**Impacto: MEDIUM-HIGH (2-5x eliminando heap fetches (index-only scan))**  
Tags: indexes, covering-index, include, index-only-scan  
Arquivo: `rules/query-covering-indexes.md`

Quando a query só lê colunas presentes no índice, o Postgres faz **index-only scan** sem tocar a tabela. `INCLUDE` adiciona colunas ao índice sem fazê-las parte da chave.

**Incorreto (index scan + heap fetch por linha):**

```sql
-- Badge de "não lidas": índice acha as linhas, mas busca cada uma na tabela
SELECT count(*) FROM "Notificacao"
WHERE "EmpresaId" = @e AND "DestinatarioUsuarioId" = @u AND "LidaEm" IS NULL;
```

**Correto (o índice responde sozinho):**

```sql
CREATE INDEX "IX_Notificacao_NaoLidas"
    ON "Notificacao" ("EmpresaId", "DestinatarioUsuarioId")
    INCLUDE ("Titulo", "Prioridade", "CreatedAt")
    WHERE "LidaEm" IS NULL;
```

```csharp
// Npgsql EF Core: IncludeProperties
builder.HasIndex(x => new { x.EmpresaId, x.DestinatarioUsuarioId })
    .IncludeProperties(x => new { x.Titulo, x.Prioridade, x.CreatedAt })
    .HasFilter("\"LidaEm\" IS NULL");
```

E a query precisa **projetar** só essas colunas (`.Select(n => new { n.Titulo, n.Prioridade, n.CreatedAt })`) — `SELECT *` via entidade completa anula o benefício.

Index-only scan depende do visibility map estar em dia (VACUUM): veja `Heap Fetches` no `EXPLAIN (ANALYZE)`. Em tabela com muito UPDATE o ganho cai.

**No TruckFlow:** `NotificacaoConfiguracao.cs` já tem o índice parcial `(EmpresaId, DestinatarioUsuarioId) WHERE "LidaEm" IS NULL` usado pelo `unread-count` (polling + `refetchOnWindowFocus` no front e mobile — é query frequente).

Referência: [Index-Only Scans and Covering Indexes](https://www.postgresql.org/docs/16/indexes-index-only-scans.html) · [Npgsql — Index include](https://www.npgsql.org/efcore/modeling/indexes.html)

---

## Gestão de conexões

**Impacto:** CRITICAL  
Pool do Npgsql, limites do servidor, PgBouncer (ADR-0004 item 8) e o que quebra em modo transaction (prepared statements, LISTEN).

### 2.1 Calcule o orçamento de conexões por componente

**Impacto: CRITICAL (previne exaustão de conexões e OOM do servidor)**  
Tags: connections, max-connections, limits, capacity  
Arquivo: `rules/conn-limits.md`

`max_connections` é recurso finito e caro. Faça a conta explícita antes de escalar instâncias ou adicionar worker.

**Incorreto (sem orçamento):**

```sql
-- "Aumenta pra 1000 que resolve"
ALTER SYSTEM SET max_connections = 1000; -- cada backend reserva memória; work_mem × conexões explode
```

**Correto (orçamento somado):**

```text
Componente                         Conexões
API (3 instâncias × pool 30)          90  (pico real bem menor — medir)
Workers (outbox/push/receipt/expiração) usam o mesmo pool por instância
LISTEN SSE (1 por instância)            3
Job de migration                        1
Admin/psql/monitoramento                5
Reserva superusuário (default)          3
-----------------------------------------
max_connections ≥ 102 → com PgBouncer, servidor fica em ~40 e o pooler absorve o resto
```

```sql
-- Uso atual por aplicação/estado
SELECT application_name, state, count(*)
FROM pg_stat_activity
GROUP BY 1, 2 ORDER BY 3 DESC;
```

Use `Application Name=truckflow-api` / `truckflow-worker` na connection string para essa query ser útil.

**No TruckFlow:** ADR-0001 estima 40-80 admins + 300-400 motoristas simultâneos no pico — requests HTTP curtas, não conexões persistentes. O consumo real é dominado pelo pool × instâncias. ADR-0004 item 8 (PgBouncer) é pré-rollout.

Referência: [Connections and Authentication](https://www.postgresql.org/docs/16/runtime-config-connection.html) · [Cybertec — max_connections tuning](https://www.cybertec-postgresql.com/en/tuning-max_connections-in-postgresql/)

---

### 2.2 Dimensione o pool do Npgsql e planeje o PgBouncer com os caveats do modo transaction

**Impacto: CRITICAL (evita "too many connections" com 2+ instâncias; 10-50x mais clientes com PgBouncer)**  
Tags: connection-pooling, npgsql, pgbouncer, multi-instance  
Arquivo: `rules/conn-pooling.md`

O Npgsql já tem pool **por processo** (default `Maximum Pool Size=100`). Com N instâncias da API + workers + LISTEN dedicado, o total pode passar do `max_connections` do Postgres managed (muitas vezes 100). Cada conexão Postgres custa ~5-10MB.

**Incorreto (defaults multiplicados por instância):**

```text
# 3 instâncias × 100 (default) = 300 conexões possíveis → servidor com max_connections=100 cai sob pico
Host=db;Database=truckflow;Username=truckflow;Password=...
```

**Correto (pool explícito, somando < max_connections com folga):**

```text
Host=db;Database=truckflow;Username=truckflow_app;Password=...;
Maximum Pool Size=30;Minimum Pool Size=2;Connection Idle Lifetime=60;
Timeout=15;Command Timeout=30;Keepalive=30;Application Name=truckflow-api
```

```csharp
// Program.cs — AddDbContext já usa o pool do Npgsql; considere DbContext pooling para reduzir alocação:
builder.Services.AddDbContextPool<AppDbContext>(...); // ATENÇÃO: só se AppDbContext não guardar estado por request
```

`AddDbContextPool` **não** é compatível com o `AppDbContext` atual sem ajuste: ele recebe `IEmpresaContext` (scoped) no construtor e interceptors scoped — pool de contexto reaproveitaria o tenant do request anterior. Mantenha `AddDbContext` (é o seguro).

**PgBouncer (ADR-0004 item 8) em modo `transaction`:**
- Session state não sobrevive entre transações: `SET`, advisory lock de sessão, `LISTEN` → **quebram**.
- Prepared statements: Npgsql faz auto-prepare só se `Max Auto Prepare > 0` (default 0 — ok). PgBouncer ≥ 1.21 suporta `max_prepared_statements` se precisar.
- Use `No Reset On Close=true` quando atrás do PgBouncer.
- O `RealtimeNotificationListener` (LISTEN) precisa de **conexão direta** ao Postgres (connection string separada, sem PgBouncer).

**No TruckFlow:** `Program.cs:130` `UseNpgsql(GetConnectionString("DefaultConnection"))` sem parâmetros de pool; listener SSE em `RealtimeNotificationListener` com `NpgsqlConnection` dedicada.

Referência: [Npgsql — Connection String Parameters](https://www.npgsql.org/doc/connection-string-parameters.html) · [Npgsql — Compatibility with PgBouncer](https://www.npgsql.org/doc/compatibility.html#pgbouncer) · [PgBouncer features](https://www.pgbouncer.org/features.html) · [EF Core — DbContext pooling](https://learn.microsoft.com/ef/core/performance/advanced-performance-topics#dbcontext-pooling)

---

### 2.3 Configure timeouts para transação ociosa e statements longos

**Impacto: HIGH (libera locks e conexões presos por request abortado)**  
Tags: connections, timeout, idle-in-transaction, statement-timeout  
Arquivo: `rules/conn-idle-timeout.md`

Uma transação aberta e esquecida (`idle in transaction`) segura locks e impede o VACUUM de limpar linhas mortas. Request cancelado no meio de `BeginTransactionAsync` sem `await using` é a fonte clássica.

**Incorreto (sem limites):**

```sql
-- conexões "idle in transaction" há horas segurando lock em "Agendamento"
SELECT pid, state, now() - xact_start AS duracao, query
FROM pg_stat_activity WHERE state = 'idle in transaction';
```

```csharp
var tx = await _db.Database.BeginTransactionAsync(ct); // sem await using: exceção deixa tx pendurada até o pool reciclar
```

**Correto (limites no role da aplicação + dispose garantido):**

```sql
ALTER ROLE truckflow_app SET idle_in_transaction_session_timeout = '30s';
ALTER ROLE truckflow_app SET statement_timeout = '30s';
ALTER ROLE truckflow_app SET lock_timeout = '5s';
```

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
// ...
await tx.CommitAsync(ct);
```

Defina no **role da aplicação**, não globalmente: o job de migration (`migration-lock-timeout`) e relatórios precisam de valores diferentes. `Command Timeout` do Npgsql é o equivalente do lado cliente.

**No TruckFlow:** o padrão `await using var tx = await _repo.BeginTransactionAsync(token)` em `AgendamentoMotoristaService.BookAppointment` está correto — siga-o.

Referência: [Client Connection Defaults](https://www.postgresql.org/docs/16/runtime-config-client.html) · [Laurenz Albe — idle in transaction](https://www.cybertec-postgresql.com/en/idle_in_transaction_session_timeout-terminating-idle-transactions-in-postgresql/)

---

### 2.4 Entenda prepared statements do Npgsql antes de ligar PgBouncer

**Impacto: MEDIUM (evita erros "prepared statement does not exist" com pooler)**  
Tags: prepared-statements, npgsql, pgbouncer, auto-prepare  
Arquivo: `rules/conn-prepared-statements.md`

Npgsql suporta prepared statements explícitos e **auto-prepare** (`Max Auto Prepare=N`), que dá 10-30% em queries repetidas. Mas prepared statements são estado de **sessão**: em PgBouncer modo transaction, a próxima transação pode cair em outro backend.

**Incorreto (auto-prepare + PgBouncer transaction antigo):**

```text
Host=pgbouncer;Max Auto Prepare=20;...   # PgBouncer < 1.21 → "prepared statement _auto0 does not exist"
```

**Correto (escolha consciente):**

```text
# Direto no Postgres (sem pooler): pode ganhar com auto-prepare
Host=db;Max Auto Prepare=20;Auto Prepare Min Usages=5;...

# Atrás de PgBouncer transaction mode:
Host=pgbouncer;Max Auto Prepare=0;No Reset On Close=true;...
# ou PgBouncer >= 1.21 com max_prepared_statements=200 no pgbouncer.ini
```

O EF Core já parametriza todas as queries — o plano é cacheado do lado do servidor só se a query for preparada. Sem auto-prepare, você ainda ganha cache de query compilada do EF (lado cliente).

**No TruckFlow:** hoje sem auto-prepare e sem PgBouncer. Ao implementar ADR-0004 item 8, decida isso **junto** e teste o `RealtimeNotificationListener` (LISTEN) fora do pooler.

Referência: [Npgsql — Prepared Statements](https://www.npgsql.org/doc/prepare.html) · [PgBouncer 1.21 prepared statements](https://www.pgbouncer.org/config.html#max_prepared_statements)

---

## Segurança & multi-tenant

**Impacto:** CRITICAL  
Isolamento entre empresas via `IEmpresaScoped` + `HasQueryFilter` + `EmpresaScopedSaveChangesInterceptor`, escapes explícitos `*AcrossTenants`, RLS como defesa em profundidade e privilégios mínimos.

### 3.1 IgnoreQueryFilters só em métodos *AcrossTenants, seguidos de WithTenant

**Impacto: CRITICAL (escape cross-tenant auditável e mínimo)**  
Tags: multi-tenant, ignore-query-filters, security, workers  
Arquivo: `rules/security-ignore-query-filters.md`

Há casos legítimos de ler fora do tenant atual: motorista (que não pertence a uma fábrica) reservando vaga, workers que varrem todas as empresas, claims de fila. Esses escapes precisam ser **explícitos no nome** e **curtos**: achar a linha, descobrir o `EmpresaId` dela, e entrar no tenant com `WithTenant` para o resto da operação.

**Incorreto (IgnoreQueryFilters solto no service):**

```csharp
var vaga = await _db.Agendamento.IgnoreQueryFilters()
    .Include(a => a.Grade)                 // tudo daqui pra frente é cross-tenant
    .FirstOrDefaultAsync(a => a.Id == id, ct);
var outras = await _db.Agendamento.IgnoreQueryFilters()
    .Where(a => a.Status == StatusAgendamento.Disponivel).ToListAsync(ct); // lista de TODAS as fábricas
```

**Correto (método nomeado no repositório + escopo de tenant):**

```csharp
// Repositório
public Task<Agendamento?> GetByIdAcrossTenants(Guid id, CancellationToken ct) =>
    _db.Agendamento.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id, ct);

// Service
var vaga = await _repo.GetByIdAcrossTenants(dto.AgendamentoId, ct)
    ?? throw new NotFoundException("Vaga não encontrada.");
using var tenantScope = _empresaContext.WithTenant(vaga.EmpresaId);
// a partir daqui: filtros e interceptor valem para a empresa da vaga
```

```sql
-- SQL cru de fila também precisa de IgnoreQueryFilters (FromSql + filtro global = WHERE extra)
SELECT *, xmin FROM "NotificacaoEntrega"
WHERE "Status" = 0 AND "Canal" = 2
ORDER BY "CreatedAt" FOR UPDATE SKIP LOCKED LIMIT 1;
```

Revisão de PR: todo `IgnoreQueryFilters()` novo precisa de (1) sufixo `AcrossTenants` ou ser claim de worker, (2) justificativa no PR, (3) teste cross-tenant.

**No TruckFlow:** `AgendamentoMotoristaService.BookAppointment` (padrão correto), `ObterPorChaveAcrossTenantsAsync` (NF-e), `NotificacaoEntregaRepositorio.ClaimNextPushPendenteAsync`, `AgendamentoExpirationWorker` (itera empresas com `WithTenant`).

Referência: [EF Core — Disabling filters](https://learn.microsoft.com/ef/core/querying/filters#disabling-filters)

---

### 3.2 Toda entidade de tenant implementa IEmpresaScoped — o filtro global falha fechado

**Impacto: CRITICAL (elimina vazamento entre empresas por esquecimento de Where)**  
Tags: multi-tenant, query-filter, empresa-scoped, security, ef-core  
Arquivo: `rules/security-tenant-query-filter.md`

Isolamento por `Where(x => x.EmpresaId == empresaId)` manual depende de ninguém esquecer. No TruckFlow o isolamento é **estrutural**: a entidade implementa `IEmpresaScoped`, o `AppDbContext` aplica `HasQueryFilter` por reflexão e o `EmpresaScopedSaveChangesInterceptor` bloqueia escrita cross-tenant. Sem tenant no contexto, a query **lança** (falha fechada) — nunca devolve tudo.

**Incorreto (entidade nova sem a interface; filtro manual):**

```csharp
public class Transportadora : EntidadeBase   // esqueceu IEmpresaScoped
{
    public Guid EmpresaId { get; set; }
}
// service
var lista = await _db.Transportadora.Where(t => t.EmpresaId == empresaId).ToListAsync(ct); // o próximo dev esquece
```

```sql
-- o que vaza quando alguém esquece o Where:
SELECT * FROM "Transportadora";  -- todas as 21 fábricas
```

**Correto (interface + filtro global + índice com EmpresaId):**

```csharp
public class Transportadora : EntidadeBase, IEmpresaScoped
{
    public Guid EmpresaId { get; set; }
    public Empresa? Empresa { get; set; }
}
// AppDbContext.SetGlobalFilter<TEntity> já aplica:
//   .HasQueryFilter(e => e.EmpresaId == _empresaContext.EmpresaId)
var lista = await _db.Transportadora.ToListAsync(ct); // SQL sai com WHERE "EmpresaId" = @__ef_filter__EmpresaId
```

```sql
-- SQL gerado
SELECT t.* FROM "Transportadora" AS t WHERE t."EmpresaId" = @__ef_filter__EmpresaId_0;
```

**Não regredir o fail-closed (ADR-0004 item 6):** o bug histórico era o fallback do filtro retornar `true` quando `EmpresaId == Guid.Empty`. Hoje `EmpresaContext.EmpresaId` lança `UnauthorizedAccessException("Tenant não identificado.")` quando não há claim nem override. Nunca troque isso por `?? Guid.Empty`, `|| empresaId == Guid.Empty` ou similar.

Teste obrigatório para entidade nova (Postgres real, `TruckFlow.Test/Integration`): criar dados em A e B, consultar como B → vazio; `GetById` de registro de A como B → `NotFoundException`.

**No TruckFlow:** `TruckFlow.Domain/Contracts/IEmpresaScoped.cs`, `AppDbContext.SetGlobalFilter`, `Database/Interceptors/EmpresaScopedSaveChangesInterceptor.cs`, `TruckFlow.Domain/Entities/EmpresaContext.cs`. Exceção conhecida: `Usuario` (Identity) **não** é `IEmpresaScoped` — toda query de usuário filtra `EmpresaId` à mão (e há gaps em `UsuarioService.SetAdminStatusAsync`/`GetAdminByIdAsync`).

Referência: [EF Core — Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters) · [Microsoft — Multi-tenancy with EF Core](https://learn.microsoft.com/ef/core/miscellaneous/multitenancy)

---

### 3.3 Role da aplicação com privilégio mínimo; migrations com outro role

**Impacto: HIGH (limita o estrago de SQL injection ou credencial vazada)**  
Tags: privileges, roles, least-privilege, migrations, security  
Arquivo: `rules/security-privileges.md`

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

---

### 3.4 RLS como defesa em profundidade (opcional) — com o caveat de pooling

**Impacto: MEDIUM-HIGH (barreira no banco contra SQL cru, ExecuteUpdate e bugs de filtro)**  
Tags: rls, row-level-security, multi-tenant, defense-in-depth, pgbouncer  
Arquivo: `rules/security-rls-defense-in-depth.md`

O filtro global do EF **não protege**: SQL cru (`FromSqlRaw` sem `WHERE`), `ExecuteUpdate/Delete` que esqueceram o tenant, psql de suporte, outra aplicação no mesmo banco. Row-Level Security no Postgres fecha essas portas — mas exige setar o tenant **por transação** na conexão.

**Incorreto (RLS com SET de sessão + pool):**

```sql
SET app.empresa_id = '...';  -- vale para a SESSÃO: a conexão volta ao pool com o tenant do request anterior
```

**Correto (policy + SET LOCAL por transação):**

```sql
ALTER TABLE "Agendamento" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "Agendamento" FORCE ROW LEVEL SECURITY; -- vale até para o dono da tabela

CREATE POLICY tenant_isolation ON "Agendamento"
    USING ("EmpresaId" = current_setting('app.empresa_id', true)::uuid)
    WITH CHECK ("EmpresaId" = current_setting('app.empresa_id', true)::uuid);
-- current_setting(..., true) devolve NULL sem setting → comparação NULL → zero linhas (fail-closed)
```

```csharp
// DbConnectionInterceptor / DbTransactionInterceptor: no início de cada transação
await using var cmd = connection.CreateCommand();
cmd.CommandText = "SELECT set_config('app.empresa_id', @e, true)"; // true = LOCAL à transação
cmd.Parameters.Add(new NpgsqlParameter("e", empresaId.ToString()));
await cmd.ExecuteNonQueryAsync(ct);
```

Custos e caveats:
- `SET LOCAL`/`set_config(..., true)` só vale **dentro de transação** — queries fora de transação explícita precisariam ser embrulhadas.
- Workers cross-tenant e claims de fila precisam de role com `BYPASSRLS` ou policy específica.
- Performance: a policy vira predicado em toda query — mantenha `EmpresaId` como coluna líder dos índices.

**No TruckFlow:** não implementado hoje (isolamento é via EF). Candidato para resposta forte no SAD Aurora (AppSec/DataSec) e pré-pentest (ADR-0004 item 16). Avalie com `risk-manager` antes.

Referência: [Row Security Policies](https://www.postgresql.org/docs/16/ddl-rowsecurity.html) · [Crunchy Data — Row Level Security for tenants](https://www.crunchydata.com/blog/row-level-security-for-tenants-in-postgres) · [Laurenz Albe — RLS and connection pools](https://www.cybertec-postgresql.com/en/row-level-security-with-connection-pooling/)

---

## Migrations seguras

**Impacto:** CRITICAL  
Mudança de schema sem downtime em tabela quente, com mobile já publicado consumindo o contrato e 2+ instâncias subindo. Fora do startup (ADR-0004 item 2).

### 4.1 Crie índice em tabela quente com CONCURRENTLY (fora da transação da migration)

**Impacto: CRITICAL (zero bloqueio de escrita em vez de minutos com a tabela travada)**  
Tags: migrations, create-index-concurrently, ef-core, zero-downtime  
Arquivo: `rules/migration-concurrent-index.md`

`CREATE INDEX` comum pega lock `SHARE` — bloqueia INSERT/UPDATE/DELETE até terminar. Em `"Agendamento"`, `"Notificacao"` ou `"MotoristaPosicaoHistorico"` em horário de pico, isso para a portaria das 21 fábricas. `CONCURRENTLY` não bloqueia escrita, mas **não pode rodar dentro de transação** — e o EF embrulha cada migration numa.

**Incorreto (o que `HasIndex` gera por padrão):**

```csharp
// migration gerada a partir de builder.HasIndex(...)
migrationBuilder.CreateIndex(
    name: "IX_Notificacao_EmpresaId_Prioridade",
    table: "Notificacao",
    columns: new[] { "EmpresaId", "Prioridade" });
```

```sql
CREATE INDEX "IX_Notificacao_EmpresaId_Prioridade" ON "Notificacao" ("EmpresaId", "Prioridade");
-- tabela travada para escrita durante todo o build
```

**Correto (SQL explícito, sem transação):**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """CREATE INDEX CONCURRENTLY IF NOT EXISTS "IX_Notificacao_EmpresaId_Prioridade" ON "Notificacao" ("EmpresaId", "Prioridade");""",
        suppressTransaction: true);
}
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """DROP INDEX CONCURRENTLY IF EXISTS "IX_Notificacao_EmpresaId_Prioridade";""",
        suppressTransaction: true);
}
```

Mantenha o `HasIndex(...)` na configuração (o model snapshot precisa saber do índice) e **substitua** o `CreateIndex` gerado pelo `Sql(...)`. Se o `CONCURRENTLY` falhar no meio, fica um índice `INVALID` — o `IF NOT EXISTS` não o recria; dropar e rodar de novo:

```sql
SELECT indexrelid::regclass FROM pg_index WHERE NOT indisvalid;
```

Tabela pequena (cadastros: `"LocalDescarga"`, `"Fornecedor"`) pode usar o `CreateIndex` normal — o lock dura milissegundos.

**No TruckFlow:** migrations em `TruckFlowApi.Infra/Migrations/`; o padrão `migrationBuilder.Sql` já é usado em `20260510033426_agendamento-overlap-exclusion.cs`.

Referência: [CREATE INDEX CONCURRENTLY](https://www.postgresql.org/docs/16/sql-createindex.html#SQL-CREATEINDEX-CONCURRENTLY) · [EF Core — Custom migration operations / suppressTransaction](https://learn.microsoft.com/ef/core/managing-schemas/migrations/managing#arbitrary-changes-via-raw-sql) · [Braintree — Safe operations for high volume PostgreSQL](https://medium.com/paypal-tech/postgresql-at-scale-database-schema-changes-without-downtime-20d3749ed680)

---

### 4.2 Mudanças de schema em expand/contract — nunca renomear/remover em um passo

**Impacto: CRITICAL (zero erro em mobile já publicado e em instâncias antigas durante rolling deploy)**  
Tags: migrations, expand-contract, zero-downtime, not-null, rename, mobile  
Arquivo: `rules/migration-expand-contract.md`

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

---

### 4.3 Use lock_timeout em migrations para não enfileirar o tráfego atrás de um ALTER

**Impacto: CRITICAL (evita parada total causada por fila de locks atrás de DDL)**  
Tags: migrations, lock-timeout, ddl, access-exclusive  
Arquivo: `rules/migration-lock-timeout.md`

Até `ALTER TABLE ... ADD COLUMN` instantâneo precisa de `ACCESS EXCLUSIVE` por um instante. Se uma transação longa (relatório, worker) segura a tabela, o `ALTER` **espera** — e todo SELECT/INSERT novo entra na fila **atrás** dele. A API para inteira enquanto o DDL nem começou.

**Incorreto (DDL sem limite de espera):**

```sql
ALTER TABLE "Agendamento" ADD COLUMN "Observacao" text;
-- esperando lock... enquanto isso 300 requests em fila
```

**Correto (falhar rápido e tentar de novo):**

```sql
SET lock_timeout = '3s';
SET statement_timeout = '60s';
ALTER TABLE "Agendamento" ADD COLUMN "Observacao" text;
```

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("SET lock_timeout = '3s';");
    migrationBuilder.AddColumn<string>(name: "Observacao", table: "Agendamento", type: "text", nullable: true);
}
```

`SET` (sem `LOCAL`) vale para a sessão da migration — ok, o job de migration é uma conexão própria. O pipeline deve ter retry com backoff se a migration falhar por `lock_timeout` (SQLSTATE `55P03`).

Operações que reescrevem a tabela (evitar em tabela quente): mudar tipo de coluna incompatível, `ADD COLUMN ... DEFAULT volatile()`, `VACUUM FULL`, `CLUSTER`. `ADD COLUMN ... DEFAULT constante` é instantâneo desde o PG 11.

**No TruckFlow:** junto de `migration-out-of-startup` — só faz sentido com migration em job separado com retry.

Referência: [Explicit Locking — table-level lock modes](https://www.postgresql.org/docs/16/explicit-locking.html) · [Xata/pgroll — lock queues](https://xata.io/blog/migrations-and-exclusive-locks) · [depesz — lock_timeout](https://www.depesz.com/2019/09/26/how-to-run-short-alter-table-without-long-locking-concurrent-queries/)

---

### 4.4 Rode migrations em job separado do pipeline, nunca no startup da API

**Impacto: CRITICAL (elimina corrida entre instâncias por __EFMigrationsHistory e boot travado)**  
Tags: migrations, deploy, multi-instance, program-cs, railway  
Arquivo: `rules/migration-out-of-startup.md`

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

---

### 4.5 Revise o SQL gerado de toda migration antes do merge

**Impacto: HIGH (pega drop/recreate acidental, rewrite de tabela e FK sem índice antes de produção)**  
Tags: migrations, review, ef-core, migrations-script  
Arquivo: `rules/migration-review-sql.md`

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

---

## Design de schema

**Impacto:** HIGH  
Tipos (`uuid`, `timestamptz`, `numeric`, `jsonb`), chaves, constraints que garantem regra de negócio (exclusion constraint), particionamento para retenção LGPD.

### 5.1 Garanta "sem sobreposição de horário por doca" com exclusion constraint, não com SELECT antes do INSERT

**Impacto: CRITICAL (impossível dupla reserva da mesma doca, mesmo com N instâncias concorrentes)**  
Tags: exclusion-constraint, btree-gist, tstzrange, concurrency, integrity  
Arquivo: `rules/schema-exclusion-constraint.md`

"Verificar conflito e depois inserir" tem janela de corrida: duas transações checam ao mesmo tempo, ambas não acham conflito, ambas gravam. Unique index não resolve sobreposição de **intervalo**. Exclusion constraint resolve no banco, atomicamente.

**Incorreto (só checagem na aplicação):**

```csharp
var conflitos = await _repo.GetConflitosAsync(localDescargaId, inicio, fim, ct);
if (conflitos.Any()) throw new BusinessException("Conflito de horário.");
await _repo.AddAgendamento(novo, ct); // outra instância fez o mesmo no mesmo milissegundo
```

**Correto (constraint + checagem amigável + tradução do erro):**

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;
ALTER TABLE "Agendamento"
ADD CONSTRAINT "agendamento_no_overlap_per_doca"
EXCLUDE USING gist (
    "LocalDescargaId" WITH =,
    tstzrange("DataInicio", "DataFim", '[)') WITH &&
)
WHERE ("LocalDescargaId" IS NOT NULL AND "StatusAgendamento" IN (0, 1, 2, 3));
```

```csharp
// pré-check continua para mensagem boa no caminho feliz; a constraint é a garantia
try
{
    await _repo.AddAgendamento(novo, ct);
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation }) // "23P01"
{
    throw new BusinessException("Conflito de horário: já existe agendamento nessa doca nesse intervalo.");
}
```

Detalhes que importam: `'[)'` (fim aberto) permite 14:00-15:00 seguido de 15:00-16:00; o `WHERE` com status faz cancelados/expirados (4,5,6) liberarem o horário — **se mudar o enum, mude o predicado** em migration; `btree_gist` é necessário para usar `=` em uuid dentro de GiST.

**No TruckFlow:** migration `20260510033426_agendamento-overlap-exclusion.cs`; tradução de `23P01` em `AgendamentoMotoristaService.BookAppointment`, `AgendamentoAdminService` e `GradeService.CreateGrade`. Use `PostgresErrorCodes.ExclusionViolation`/`UniqueViolation` em vez de string literal em código novo.

Referência: [Exclusion Constraints](https://www.postgresql.org/docs/16/ddl-constraints.html#DDL-CONSTRAINTS-EXCLUSION) · [btree_gist](https://www.postgresql.org/docs/16/btree-gist.html) · [Range Types](https://www.postgresql.org/docs/16/rangetypes.html) · [Haki Benita — Exclusion constraints](https://hakibenita.com/postgresql-unknown-features#prevent-overlapping-ranges-using-an-exclusion-constraint)

---

### 5.2 Escolha tipos explícitos — numeric para peso/valor, text com limite de domínio, enum como int estável

**Impacto: HIGH (corretude fiscal, índices menores, sem conversão implícita)**  
Tags: data-types, schema, numeric, enum, ef-core  
Arquivo: `rules/schema-data-types.md`

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

---

### 5.3 FKs têm índice (o EF cria) — mas verifique FKs criadas por SQL cru e índices redundantes

**Impacto: HIGH (10-100x em JOIN/DELETE em cascata; evita lock longo ao deletar pai)**  
Tags: foreign-keys, indexes, cascade, ef-core  
Arquivo: `rules/schema-foreign-key-indexes.md`

O Postgres **não** indexa FK automaticamente; o EF Core sim (convenção `IX_<Tabela>_<Fk>`). Os furos aparecem quando a FK nasce em `migrationBuilder.Sql(...)` (expand/contract com `NOT VALID`), em tabela particionada, ou quando um índice composto torna o simples redundante.

**Incorreto (FK por SQL sem índice):**

```sql
ALTER TABLE "Agendamento" ADD CONSTRAINT "FK_Agendamento_Transportadora_TransportadoraId"
    FOREIGN KEY ("TransportadoraId") REFERENCES "Transportadora"("Id") NOT VALID;
-- DELETE FROM "Transportadora" → seq scan em "Agendamento" para checar referências
```

**Correto:**

```sql
CREATE INDEX CONCURRENTLY "IX_Agendamento_TransportadoraId" ON "Agendamento" ("TransportadoraId");
```

```csharp
builder.HasOne(x => x.Transportadora).WithMany().HasForeignKey(x => x.TransportadoraId)
    .OnDelete(DeleteBehavior.Restrict); // cascata em dado fiscal/operacional é perigosa
// HasIndex é implícito pela convenção — explicite se for composto
```

FKs sem índice:

```sql
SELECT c.conrelid::regclass AS tabela, a.attname AS coluna
FROM pg_constraint c
JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = ANY(c.conkey)
WHERE c.contype = 'f'
  AND NOT EXISTS (SELECT 1 FROM pg_index i
                  WHERE i.indrelid = c.conrelid AND a.attnum = i.indkey[0]);
```

Índices nunca usados (candidatos a remover):

```sql
SELECT relname, indexrelname, idx_scan FROM pg_stat_user_indexes
WHERE idx_scan = 0 ORDER BY pg_relation_size(indexrelid) DESC;
```

**No TruckFlow:** `DeleteBehavior` — Agendamento/Grade são hard-deleted hoje; prefira `Restrict` para tudo que tem rastro fiscal (`NotaFiscal`, `RecebimentoEvento`, `AuditLog`).

Referência: [Use The Index, Luke — FK](https://use-the-index-luke.com/sql/where-clause/the-equals-operator/foreign-key) · [EF Core — Indexes by convention](https://learn.microsoft.com/ef/core/modeling/indexes#index-on-foreign-keys) · [Cybertec — FK indexes](https://www.cybertec-postgresql.com/en/index-your-foreign-key/)

---

### 5.4 Particione o histórico de posição por mês e cumpra a retenção LGPD com DROP de partição

**Impacto: HIGH (retenção de 90 dias em O(1) (DROP) em vez de DELETE de milhões de linhas + bloat)**  
Tags: partitioning, range, retention, lgpd, tracking, no-timescaledb  
Arquivo: `rules/schema-partitioning.md`

`MotoristaPosicaoHistorico` (~14 inserts/s, ~100M linhas em 90 dias) precisa apagar dado velho por obrigação LGPD. `DELETE ... WHERE "CapturadoEm" < now() - '90 days'` gera milhões de tuplas mortas, WAL enorme e VACUUM pesado. Partição por intervalo de tempo transforma retenção em `DROP TABLE` instantâneo. **Sem TimescaleDB** (revisão em `Docs/rastreamento-motorista-backlog.md`: hospedagem no Brasil não pode depender de extensão).

**Incorreto:**

```sql
CREATE TABLE "MotoristaPosicaoHistorico" (... "CapturadoEm" timestamptz NOT NULL);
-- job diário:
DELETE FROM "MotoristaPosicaoHistorico" WHERE "CapturadoEm" < now() - interval '90 days';
```

**Correto (particionamento declarativo por mês):**

```sql
CREATE TABLE "MotoristaPosicaoHistorico" (
    "MotoristaId"   uuid NOT NULL,
    "AgendamentoId" uuid NOT NULL,
    "EmpresaId"     uuid NOT NULL,
    "Latitude"      double precision NOT NULL,
    "Longitude"     double precision NOT NULL,
    "Accuracy"      real NOT NULL,
    "Velocidade"    real NULL,
    "CapturadoEm"   timestamptz NOT NULL,
    PRIMARY KEY ("MotoristaId", "CapturadoEm")          -- PK precisa conter a chave de partição
) PARTITION BY RANGE ("CapturadoEm");

CREATE TABLE "MotoristaPosicaoHistorico_2026_10" PARTITION OF "MotoristaPosicaoHistorico"
    FOR VALUES FROM ('2026-10-01') TO ('2026-11-01');
CREATE INDEX ON "MotoristaPosicaoHistorico" ("EmpresaId", "AgendamentoId", "CapturadoEm"); -- propaga às partições

-- job mensal (worker com SKIP LOCKED/advisory lock): cria o mês+2 e dropa o que passou de 90 dias
ALTER TABLE "MotoristaPosicaoHistorico" DETACH PARTITION "MotoristaPosicaoHistorico_2026_06" CONCURRENTLY;
DROP TABLE "MotoristaPosicaoHistorico_2026_06";
```

```csharp
// EF: mapeie a tabela pai normalmente; DDL de partição vai em migrationBuilder.Sql(...)
builder.ToTable(nameof(MotoristaPosicaoHistorico));
builder.HasKey(x => new { x.MotoristaId, x.CapturadoEm });
// insert do hot path: batch sem tracking (ver data-batch-inserts), não AddRange + SaveChanges
```

Granularidade mensal + retenção 90 dias = apaga "o mês que saiu por inteiro" (dado fica entre 90 e ~120 dias). Se o jurídico exigir exatos 90, use partição diária ou complemente com `DELETE` só na partição mais antiga. Queries sempre com filtro em `"CapturadoEm"` para **partition pruning**. Considere BRIN em `"CapturadoEm"` dentro de cada partição.

**No TruckFlow:** tabela ainda não existe (ADR-0003 + revisão). Direito de exclusão LGPD (`DELETE /v1/motorista/eu/historico-localizacao`) é `DELETE ... WHERE "MotoristaId" = $1` — use a PK (`MotoristaId` líder).

Referência: [Table Partitioning](https://www.postgresql.org/docs/16/ddl-partitioning.html) · [Crunchy Data — Native partitioning / pg_partman](https://www.crunchydata.com/blog/native-partitioning-with-postgres) · [Laurenz Albe — Partitioning pitfalls](https://www.cybertec-postgresql.com/en/partitioning-as-a-query-optimization-strategy/)

---

### 5.5 Instantes em timestamptz com DateTime UTC; fuso de São Paulo só nas bordas

**Impacto: HIGH (elimina erro de 3h em janela de agendamento e exceções do Npgsql 6+)**  
Tags: timestamptz, datetime, utc, timezone, npgsql  
Arquivo: `rules/schema-timestamptz.md`

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

---

### 5.6 Variação por fábrica em Empresa.Configuracoes (jsonb) — tipada no C#, nunca if por tenant

**Impacto: MEDIUM-HIGH (21 fábricas com regras diferentes sem código condicional nem migration por variação)**  
Tags: jsonb, configuration, multi-tenant, owned-json, schema  
Arquivo: `rules/schema-jsonb-config.md`

ADR-0001: cada fábrica Aurora opera com processos parcialmente diferentes (escala de descarga, janelas, métodos de login). Isso mora em `Empresa.Configuracoes` (`jsonb`), não em `if (empresaId == ...)` nem em colunas novas para cada variação. Mas `jsonb` como `string?` cru espalha parse e chave mágica pelo código.

**Incorreto:**

```csharp
if (empresa.Cnpj == "83.310.441/0001-17") { janelaMinutos = 45; }          // regra hardcoded por fábrica
var cfg = JsonDocument.Parse(empresa.Configuracoes!);                        // parse ad-hoc
var metodos = cfg.RootElement.GetProperty("authMethods");                   // chave mágica
```

**Correto (tipo C# + mapeamento + default):**

```csharp
public sealed class EmpresaConfiguracoes
{
    public string[] AuthMethods { get; set; } = ["Local"];      // ["Local","EntraId"]
    public int JanelaDescargaMinutos { get; set; } = 60;
    public int ToleranciaAtrasoMinutos { get; set; } = 15;
}
// Configuração EF 8+/Npgsql: owned type como JSON
builder.OwnsOne(x => x.Configuracoes, c => c.ToJson());
// ou, mantendo string? por compatibilidade, um ValueConverter com JsonSerializer + defaults
```

```sql
-- consultar/filtrar (índice GIN se for usado em WHERE — ver advanced-jsonb-indexing)
SELECT "Id" FROM "Empresa" WHERE "Configuracoes" -> 'AuthMethods' ? 'EntraId';
-- atualização parcial sem sobrescrever o resto
UPDATE "Empresa" SET "Configuracoes" = jsonb_set("Configuracoes", '{JanelaDescargaMinutos}', '45')
WHERE "Id" = $1;
```

Regras: chave nova sempre com **default** no C# (empresas antigas não têm a chave); nada de dado relacional (lista de docas, produtos) dentro do jsonb — isso é tabela; mudança de config é auditada (`AuditSaveChangesInterceptor` já pega `Modified`).

**No TruckFlow:** `Empresa.Configuracoes` é `string?` com `HasColumnType("jsonb")` em `EmpresaConfiguracao.cs:73`; `authMethods` do Entra ID por tenant (`Docs/entra-id-integracao-backlog.md`) já depende disso.

Referência: [JSON Types](https://www.postgresql.org/docs/16/datatype-json.html) · [Npgsql — JSON mapping](https://www.npgsql.org/efcore/mapping/json.html) · [EF Core — JSON columns](https://learn.microsoft.com/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)

---

### 5.7 Chaves primárias Guid — prefira UUIDv7 (ordenável) para tabelas de alto insert

**Impacto: MEDIUM (menos fragmentação e WAL em índices de PK de tabelas grandes)**  
Tags: primary-keys, uuid, uuidv7, guid, schema  
Arquivo: `rules/schema-primary-keys.md`

O TruckFlow usa `Guid` gerado no cliente (`EntidadeBase.Id = Guid.NewGuid()`) — bom para multi-instância e para criar agregados com FK antes do `SaveChanges`. Mas `Guid.NewGuid()` é **v4 aleatório**: cada insert cai numa página aleatória do índice da PK, causando page splits e mais WAL. Irrelevante em cadastros; relevante em tabelas de ~14 inserts/s contínuos (rastreamento, outbox, audit).

**Incorreto (v4 em tabela append-heavy):**

```csharp
public Guid Id { get; set; } = Guid.NewGuid(); // aleatório
```

```sql
-- índice PK com inserts espalhados → bloat e cache miss
```

**Correto (UUIDv7: prefixo temporal, ordenado):**

```csharp
// .NET 9+: Guid.CreateVersion7(); no .NET 8 use a lib UUIDNext ou gere no banco
public Guid Id { get; set; } = UUIDNext.Uuid.NewDatabaseFriendly(UUIDNext.Database.PostgreSql);
```

```sql
-- PG 18 terá uuidv7() nativo; até lá, gerar no cliente
```

Não troque a PK de tabelas existentes por bigint/identity: o custo de migrar FKs e contratos (front/mobile usam `id: string` uuid) não compensa. Para tabelas **sem** identidade de negócio e enormes (histórico de posição), a PK pode ser composta `(MotoristaId, CapturadoEm)` — e precisa incluir a chave de partição (`schema-partitioning`).

**No TruckFlow:** `TruckFlow.Domain/Entities/EntidadeBase.cs`. Candidatos a v7: `OutboxEvent`, `AuditLog`, `NotificacaoEntrega`, futura `MotoristaPosicaoHistorico`.

Referência: [RFC 9562 — UUIDv7](https://www.rfc-editor.org/rfc/rfc9562) · [Brandur — Identity crisis: sequence vs UUID](https://brandur.org/nanoglyphs/026-ids) · [Cybertec — UUID performance](https://www.cybertec-postgresql.com/en/unexpected-downsides-of-uuid-keys-in-postgresql/)

---

### 5.8 Identificadores são PascalCase entre aspas — mantenha a convenção e sempre cite no SQL cru

**Impacto: MEDIUM (evita "relation does not exist" e mistura de convenções)**  
Tags: identifiers, naming, quoting, schema, ef-core  
Arquivo: `rules/schema-quoted-identifiers.md`

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

---

## Concorrência & locking

**Impacto:** MEDIUM-HIGH  
Concorrência otimista com `xmin`, transações curtas, `FOR UPDATE SKIP LOCKED` para workers em multi-instância, advisory locks, prevenção de deadlock.

### 6.1 Concorrência otimista com xmin; DbUpdateConcurrencyException vira BusinessException

**Impacto: CRITICAL (100 reservas simultâneas na mesma vaga → exatamente 1 sucesso, sem lock pessimista)**  
Tags: optimistic-concurrency, xmin, concurrency-token, ef-core, npgsql  
Arquivo: `rules/lock-optimistic-xmin.md`

Read-modify-write sem controle de versão perde atualização: dois motoristas leem a vaga `Disponivel`, ambos setam `Agendado`, o último ganha. A coluna de sistema `xmin` do Postgres muda a cada UPDATE — mapeada como concurrency token, o EF gera `UPDATE ... WHERE "Id" = @id AND xmin = @original` e lança `DbUpdateConcurrencyException` se 0 linhas mudarem. Sem coluna extra, sem trigger.

**Incorreto (last write wins):**

```csharp
var vaga = await _db.Agendamento.FirstAsync(a => a.Id == id, ct);
if (vaga.StatusAgendamento != StatusAgendamento.Disponivel) throw new BusinessException("Vaga indisponível.");
vaga.Reservar(...);
await _db.SaveChangesAsync(ct);   // UPDATE ... WHERE "Id" = @id — sobrescreve a reserva do outro
```

```sql
UPDATE "Agendamento" SET "StatusAgendamento" = 2, "UsuarioId" = $1 WHERE "Id" = $2;
```

**Correto (token + tradução para erro de negócio):**

```csharp
// AgendamentoConfiguracao.cs (padrão real)
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();

// Service
try
{
    await _repo.Update(vaga, ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException("Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
```

```sql
UPDATE "Agendamento" SET "StatusAgendamento" = 2, "UsuarioId" = $1
WHERE "Id" = $2 AND xmin = $3;   -- 0 linhas → DbUpdateConcurrencyException
```

Regras: **não** faça retry automático cego (a regra de negócio precisa ser reavaliada com dado novo); `DbUpdateConcurrencyException` não tratada vira 500 no `ExceptionHandlingMiddleware` — sempre traduza; SQL cru que lê entidade com xmin precisa selecionar `xmin` (`SELECT *, xmin FROM ...`); `ExecuteUpdateAsync` **ignora** o token.

Quando o conflito é frequente e o custo de refazer é alto (fila), prefira `FOR UPDATE SKIP LOCKED` (`lock-skip-locked`).

**No TruckFlow:** `xmin` em `Agendamento`, `ItemPlanejamento`, `Notificacao`, `NotificacaoEntrega`, `OutboxEvent`, `DispositivoUsuario` (migration `20260510025303_concurrency`). Coberto por `Integration/AgendamentoReservaConcorrenciaTests` (100 threads → 1 sucesso).

Referência: [Npgsql — Optimistic concurrency with xmin](https://www.npgsql.org/efcore/modeling/concurrency.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency) · [System Columns](https://www.postgresql.org/docs/16/ddl-system-columns.html)

---

### 6.2 Workers em multi-instância reivindicam trabalho com FOR UPDATE SKIP LOCKED

**Impacto: CRITICAL (2+ instâncias processam a fila em paralelo sem duplicar push/expiração)**  
Tags: skip-locked, queue, workers, outbox, multi-instance  
Arquivo: `rules/lock-skip-locked.md`

`BackgroundService` roda em **toda** instância da API. Sem claim atômico, duas instâncias pegam o mesmo evento do outbox e o motorista recebe 2 pushes, ou duas expiram o mesmo agendamento e uma estoura concorrência. `FOR UPDATE SKIP LOCKED` faz cada worker travar linhas diferentes sem esperar o outro.

**Incorreto (SELECT + processa + UPDATE):**

```csharp
var pendentes = await _db.OutboxEvent
    .Where(e => e.ProcessedAt == null)
    .OrderBy(e => e.CreatedAt).Take(50).ToListAsync(ct);   // as duas instâncias leem os mesmos 50
foreach (var e in pendentes) { await Processar(e); e.MarcarProcessado(); }
await _db.SaveChangesAsync(ct);
```

**Correto (claim dentro de transação curta):**

```sql
BEGIN;
SELECT *, xmin FROM "NotificacaoEntrega"
WHERE "Status" = 0 AND "Canal" = 2
  AND ("ProximaTentativaEm" IS NULL OR "ProximaTentativaEm" <= now())
ORDER BY "CreatedAt"
FOR UPDATE SKIP LOCKED
LIMIT 1;
-- processa, atualiza status/tentativas
COMMIT;
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var entrega = await _db.NotificacaoEntrega
    .FromSqlRaw("""
        SELECT *, xmin FROM "NotificacaoEntrega"
        WHERE "Status" = 0 AND "Canal" = 2
          AND ("ProximaTentativaEm" IS NULL OR "ProximaTentativaEm" <= NOW())
        ORDER BY "CreatedAt"
        FOR UPDATE SKIP LOCKED
        LIMIT 1
        """)
    .IgnoreQueryFilters()           // fila é cross-tenant; o handler entra em WithTenant(entrega.EmpresaId)
    .FirstOrDefaultAsync(ct);
// ... processa
await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);
```

Cuidados: o lock vive **só dentro da transação** — chamada HTTP longa (Expo, SEFAZ) dentro dela segura a linha; para lotes, marque `"Status" = Processando` + `"ClaimedAt"` e commite antes da chamada externa, com timeout de reclaim. Backoff via `"ProximaTentativaEm"` + índice parcial da fila (`query-partial-indexes`).

**No TruckFlow:** `OutboxProcessorWorker`, `NotificacaoEntregaRepositorio.ClaimNextPushPendenteAsync`/`ClaimLotePushPendenteReceiptAsync`. Pendente (ADR-0004 item 4): `AgendamentoExpirationService` ainda depende de `xmin` para não duplicar — migrar o SELECT para `SKIP LOCKED`. Teste: `OutboxDispatcherConcorrenciaTests`.

Referência: [SELECT — The Locking Clause](https://www.postgresql.org/docs/16/sql-select.html#SQL-FOR-UPDATE-SHARE) · [Brandur — Transactionally staged job drains](https://brandur.org/job-drain) · [Crunchy Data — Message queuing with SKIP LOCKED](https://www.crunchydata.com/blog/message-queuing-using-native-postgresql)

---

### 6.3 Transações curtas — nada de I/O externo (SEFAZ, Expo, e-mail) dentro de transação

**Impacto: HIGH (locks segurados por ms em vez de segundos; sem cascata de espera)**  
Tags: transactions, locks, external-calls, outbox  
Arquivo: `rules/lock-short-transactions.md`

Lock de linha dura até o `COMMIT`. Uma chamada à SEFAZ (timeout de 60s no `ZeusSefazClient`) dentro da transação segura a linha do agendamento — qualquer outra operação na mesma vaga espera, o pool esvazia, e um timeout de rede vira incidente no banco.

**Incorreto:**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
var vaga = await _repo.GetById(id, ct);
var status = await _sefaz.ConsultarProtocoloAsync(chave, uf, ct);   // até 60s com lock aberto
await _email.EnviarAsync(...);                                        // mais I/O externo
vaga.Reservar(...);
await _repo.Update(vaga, ct);
await tx.CommitAsync(ct);
```

**Correto (I/O antes; efeito colateral depois via outbox):**

```csharp
var status = await _sefaz.ConsultarProtocoloAsync(chave, uf, ct);    // fora da transação
await using var tx = await _repo.BeginTransactionAsync(ct);
var vaga = await _repo.GetById(id, ct);
vaga.Reservar(...);                     // levanta domain event → OutboxSaveChangesInterceptor grava OutboxEvent
await _repo.Update(vaga, ct);           // mesma transação: estado + evento atômicos
await tx.CommitAsync(ct);
// push/e-mail saem pelo OutboxProcessorWorker depois do commit
```

```sql
-- transações longas agora
SELECT pid, now() - xact_start AS duracao, state, left(query, 80)
FROM pg_stat_activity WHERE xact_start IS NOT NULL ORDER BY duracao DESC;
```

Transactional outbox é exatamente para isso: o "enviar notificação" é gravado junto com a mudança de estado e executado fora. Nunca chame `IExpoPushClient` direto de dentro de um service transacional.

**No TruckFlow:** `EntidadeBase.AddDomainEvent` + `OutboxSaveChangesInterceptor` + `OutboxProcessorWorker` (ADR-0002). `BookAppointment` já faz a transação só com `Update` + lifecycle.

Referência: [Chris Richardson — Transactional Outbox](https://microservices.io/patterns/data/transactional-outbox.html) · [Explicit Locking](https://www.postgresql.org/docs/16/explicit-locking.html)

---

### 6.4 Previna deadlock travando linhas em ordem consistente

**Impacto: MEDIUM-HIGH (elimina erros 40P01 intermitentes sob carga)**  
Tags: deadlock, locks, ordering, transactions  
Arquivo: `rules/lock-deadlock-prevention.md`

Deadlock acontece quando T1 trava A e espera B enquanto T2 trava B e espera A. O Postgres mata uma delas (`40P01 deadlock_detected`) depois de `deadlock_timeout`. Aparece em operações que tocam várias linhas: reagendar (vaga antiga + nova), atualização em lote de grade, conferência com N itens.

**Incorreto (ordem depende da entrada):**

```csharp
// Reagendar: trava a origem, depois o destino
var origem = await Travar(dto.OrigemId);
var destino = await Travar(dto.DestinoId);   // outro request reagendando destino→origem trava ao contrário
```

```sql
-- T1: UPDATE ... WHERE "Id" = 'aaa'; UPDATE ... WHERE "Id" = 'bbb';
-- T2: UPDATE ... WHERE "Id" = 'bbb'; UPDATE ... WHERE "Id" = 'aaa';  → deadlock
```

**Correto (ordem determinística por Id):**

```sql
SELECT * FROM "Agendamento" WHERE "Id" = ANY($1) ORDER BY "Id" FOR UPDATE;
```

```csharp
var ids = new[] { dto.OrigemId, dto.DestinoId }.Order().ToArray();
var vagas = await _db.Agendamento
    .FromSqlInterpolated($"""SELECT *, xmin FROM "Agendamento" WHERE "Id" = ANY({ids}) ORDER BY "Id" FOR UPDATE""")
    .ToListAsync(ct);
```

Também: atualizações em lote ordenadas por PK; transações curtas (`lock-short-transactions`); se ainda ocorrer, trate `40P01` como erro transitório com **um** retry (diferente de conflito de negócio).

```sql
SELECT deadlocks FROM pg_stat_database WHERE datname = current_database();
```

**No TruckFlow:** `AgendamentoAdminService` (reagendar/cancelar/finalizar) e `GradeView` no front atualizando grades em loop (N PATCHs) são os candidatos. Habilite `log_lock_waits = on` no servidor.

Referência: [Deadlocks](https://www.postgresql.org/docs/16/explicit-locking.html#LOCKING-DEADLOCKS) · [Cybertec — How to fix deadlocks](https://www.cybertec-postgresql.com/en/postgresql-understanding-deadlocks/)

---

### 6.5 Advisory lock (por transação) para jobs singleton entre instâncias

**Impacto: MEDIUM (um único executor de job periódico sem tabela de lock)**  
Tags: advisory-locks, distributed-lock, workers, jobs  
Arquivo: `rules/lock-advisory.md`

Alguns jobs devem rodar em **uma** instância por vez e não têm "linhas de fila" para `SKIP LOCKED`: criar/dropar partição mensal, recalcular dashboard, seed. Advisory lock é um mutex do Postgres identificado por um número.

**Incorreto (flag em memória ou tabela sem lock):**

```csharp
private static bool _rodando;            // vale por processo — 3 instâncias rodam 3 vezes
if (_rodando) return; _rodando = true;
```

**Correto (try-lock por transação):**

```sql
BEGIN;
SELECT pg_try_advisory_xact_lock(hashtext('truckflow:particoes-posicao'));  -- true só para um
-- se true: executa o job; o lock solta sozinho no COMMIT/ROLLBACK
COMMIT;
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var conseguiu = await _db.Database
    .SqlQuery<bool>($"SELECT pg_try_advisory_xact_lock(hashtext('truckflow:particoes-posicao')) AS \"Value\"")
    .SingleAsync(ct);
if (!conseguiu) return;              // outra instância está fazendo
await ExecutarManutencao(ct);
await tx.CommitAsync(ct);
```

Prefira a variante `_xact_` (por transação): advisory lock de **sessão** (`pg_advisory_lock`) não funciona atrás de PgBouncer em modo transaction e "vaza" se a conexão voltar ao pool sem unlock. Para filas com linhas, `SKIP LOCKED` é melhor que advisory.

**No TruckFlow:** candidato para o job de partições do rastreamento e para qualquer `BackgroundService` que não processe linhas individuais.

Referência: [Advisory Locks](https://www.postgresql.org/docs/16/explicit-locking.html#ADVISORY-LOCKS) · [Advisory Lock Functions](https://www.postgresql.org/docs/16/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS)

---

## Padrões de acesso a dados

**Impacto:** MEDIUM  
N+1, tracking, projeção, bulk (`ExecuteUpdate/Delete` pula interceptors!), paginação offset vs keyset, upsert, batch, idempotência.

### 7.1 ExecuteUpdate/ExecuteDelete são rápidos — e pulam interceptors (tenant, audit, outbox) e o xmin

**Impacto: CRITICAL (bulk em 1 statement; sem cuidado, escrita sem auditoria e sem guarda de tenant)**  
Tags: executeupdate, executedelete, bulk, interceptors, audit, multi-tenant  
Arquivo: `rules/data-executeupdate-delete.md`

`ExecuteUpdateAsync`/`ExecuteDeleteAsync` viram um único `UPDATE`/`DELETE` no banco, sem carregar entidades. Ótimo para lote. Mas **não passam pelo `SaveChanges`**: `EmpresaScopedSaveChangesInterceptor`, `AuditSaveChangesInterceptor` e `OutboxSaveChangesInterceptor` **não rodam**, domain events não são publicados, `UpdatedAt`/`UpdatedBy` não são preenchidos e o token `xmin` é ignorado. O filtro global de **leitura** ainda se aplica ao `WHERE` (então o tenant é respeitado no LINQ) — mas SQL cru não.

**Incorreto (bulk onde precisava de trilha):**

```csharp
// Cancelar todos os agendamentos de uma grade
await _db.Agendamento
    .Where(a => a.GradeId == gradeId)
    .ExecuteUpdateAsync(s => s.SetProperty(a => a.StatusAgendamento, StatusAgendamento.Cancelado), ct);
// sem AuditLog, sem AgendamentoCanceladoEvent → motoristas não são notificados; transição de status não validada
```

**Correto (escolha consciente):**

```csharp
// 1) Regra de negócio/evento/auditoria importam → carregar e usar o método de domínio
var vagas = await _db.Agendamento.Where(a => a.GradeId == gradeId).ToListAsync(ct);
foreach (var v in vagas) v.Cancelar("Grade removida");     // valida transição + domain event
await _db.SaveChangesAsync(ct);                            // interceptors rodam

// 2) Manutenção técnica sem semântica de negócio → bulk, com tenant e UpdatedAt explícitos
await _db.NotificacaoEntrega
    .Where(e => e.Status == StatusEntrega.Pendente && e.CreatedAt < corte)
    .ExecuteUpdateAsync(s => s
        .SetProperty(e => e.Status, StatusEntrega.Cancelado)
        .SetProperty(e => e.UpdatedAt, DateTime.UtcNow), ct);
```

```sql
UPDATE "NotificacaoEntrega" SET "Status" = 3, "UpdatedAt" = now()
WHERE "EmpresaId" = $1 AND "Status" = 0 AND "CreatedAt" < $2;
```

Checklist antes de usar bulk: o dado tem trilha de auditoria exigida (fiscal/LGPD)? Alguém precisa ser notificado (evento)? Há transição de status validada no domínio? Se sim para qualquer um → caminho 1. Lotes grandes: em fatias (`LIMIT` por Id) para não segurar lock em milhões de linhas.

**No TruckFlow:** hard deletes usam `ExecuteDeleteAsync` em alguns repositórios — confira se o registro precisava de `AuditLog`. `AgendamentoExpirationService` usa o caminho 1 (`agendamento.Expirar()` em lotes de 500) — correto.

Referência: [EF Core — ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/ef/core/saving/execute-insert-update-delete) · [EF Core — Interceptors](https://learn.microsoft.com/ef/core/logging-events-diagnostics/interceptors)

---

### 7.2 Idempotency-Key em POST críticos (reservar, posição) — padrão Stripe com unique no banco

**Impacto: HIGH (retry de rede/double-tap nunca duplica reserva ou batch de posição)**  
Tags: idempotency, idempotency-key, retries, stripe, unique-constraint  
Arquivo: `rules/data-idempotency-key.md`

Motorista em rede ruim toca "Reservar", o request chega, a resposta se perde, o app tenta de novo. Sem idempotência, o segundo request pode criar efeito duplicado ou devolver erro confuso ("vaga já reservada" — por ele mesmo). O cliente manda `Idempotency-Key: <uuid>`; o servidor grava a chave **na mesma transação** do efeito e, num replay, devolve a resposta original.

**Incorreto:**

```csharp
[HttpPost("reservar")]
public async Task<IActionResult> Reservar([FromBody] ReservarAgendamentoDto dto, CancellationToken ct)
    => Ok(await _service.BookAppointment(dto, ct));   // replay = segundo efeito ou erro enganoso
```

**Correto (tabela + unique + mesma transação):**

```sql
CREATE TABLE "IdempotencyKey" (
    "Key"            uuid        NOT NULL,
    "UsuarioId"      uuid        NOT NULL,
    "Rota"           varchar(120) NOT NULL,
    "RequestHash"    char(64)    NOT NULL,          -- sha256 do body: mesma chave + body diferente = 422
    "StatusCode"     int         NULL,
    "ResponseBody"   jsonb       NULL,
    "CriadoEm"       timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY ("UsuarioId", "Key")                -- escopo por usuário: chave de um não colide com outro
);
CREATE INDEX "IX_IdempotencyKey_CriadoEm" ON "IdempotencyKey" ("CriadoEm"); -- limpeza > 24h
```

```csharp
await using var tx = await _db.Database.BeginTransactionAsync(ct);
var existente = await _db.IdempotencyKey.FindAsync([userId, key], ct);
if (existente is { StatusCode: not null })
    return existente.RequestHash == hash ? Replay(existente) : throw new BusinessException("Idempotency-Key reutilizada com outro conteúdo.");

var registro = new IdempotencyKey { Key = key, UsuarioId = userId, Rota = "reservar", RequestHash = hash };
_db.IdempotencyKey.Add(registro);
var resultado = await _service.BookAppointment(dto, ct);          // efeito
registro.StatusCode = 200; registro.ResponseBody = JsonSerializer.Serialize(resultado);
await _db.SaveChangesAsync(ct);
await tx.CommitAsync(ct);   // chave e efeito commitam juntos; requisição concorrente com a mesma chave → 23505 → 409 "em processamento"
```

Implementar como `IEndpointFilter`/action filter reutilizável, aplicado por atributo (`[Idempotent]`). Retenção 24h com job de limpeza (`DELETE ... WHERE "CriadoEm" < now() - interval '24 hours'` em lote). Domain events já têm `IdempotencyKey` no outbox (unique em `OutboxEventConfiguracao.cs:57`) — mesmo princípio no consumo.

**No TruckFlow:** ADR-0004 item 12 (pré-piloto) — `/AgendamentoMotorista/reservar` e `POST /v1/motorista/posicao`. Mobile gera o uuid por **intenção** (mesma chave nos retries), não por tentativa.

Referência: [Brandur — Implementing Stripe-like Idempotency Keys in Postgres](https://brandur.org/idempotency-keys) · [Stripe — Idempotent requests](https://docs.stripe.com/api/idempotent_requests) · [IETF draft — Idempotency-Key header](https://datatracker.ietf.org/doc/draft-ietf-httpapi-idempotency-key-header/)

---

### 7.3 Elimine N+1 — projeção com Select para leitura, Include só quando vai mutar, AsSplitQuery para várias coleções

**Impacto: MEDIUM-HIGH (1+N round-trips → 1-2 queries; payload 3-10x menor com projeção)**  
Tags: n-plus-one, include, projection, split-query, ef-core  
Arquivo: `rules/data-n-plus-one.md`

N+1 no EF aparece de três formas: lazy loading (desligado no TruckFlow — ótimo), loop com query dentro, e acesso a navegação não carregada que "funciona" retornando null. O remédio certo depende do uso.

**Incorreto:**

```csharp
var agendamentos = await _db.Agendamento.Where(...).ToListAsync(ct);
foreach (var a in agendamentos)
{
    a.Fornecedor = await _db.Fornecedor.FindAsync(a.FornecedorId);   // 1 query por linha
}
// ou: Include de tudo para montar uma tela de listagem
var lista = await _db.Agendamento
    .Include(x => x.Fornecedor).Include(x => x.Usuario).ThenInclude(u => u.Motorista)
    .Include(x => x.NotaFiscal).ThenInclude(n => n.Itens)              // cartesian explosion
    .Include(x => x.Grade).ThenInclude(g => g.Produto)
    .ToListAsync(ct);
```

**Correto (listagem = projeção para o DTO):**

```csharp
var items = await query
    .OrderByDescending(x => x.DataInicio)
    .Skip((page - 1) * size).Take(size)
    .Select(a => new AgendamentoAdminResponse
    {
        Id = a.Id,
        FornecedorNome = a.Fornecedor!.Nome,
        MotoristaNome = a.Usuario!.Motorista!.NomeReal,
        LocalDescarga = a.LocalDescarga!.Nome,
        Status = a.StatusAgendamento.ToString(),
        DataInicio = a.DataInicio,
    })
    .ToListAsync(ct);   // 1 query, só as colunas da tela, sem tracking
```

```sql
SELECT a."Id", f."Nome", m."NomeReal", l."Nome", a."StatusAgendamento", a."DataInicio"
FROM "Agendamento" a
LEFT JOIN "Fornecedor" f ON f."Id" = a."FornecedorId"
LEFT JOIN "AspNetUsers" u ON u."Id" = a."UsuarioId"
LEFT JOIN "Motorista" m ON m."UsuarioId" = u."Id"
LEFT JOIN "LocalDescarga" l ON l."Id" = a."LocalDescargaId"
WHERE a."EmpresaId" = $1 ORDER BY a."DataInicio" DESC LIMIT $2 OFFSET $3;
```

Quando usar cada um:
- **`Select` (projeção)**: toda leitura para tela/listagem. Preferido.
- **`Include`**: quando vai **modificar** o agregado e salvar (tracking).
- **`AsSplitQuery()`**: Include de 2+ coleções (`NotaFiscal.Itens` + outra) — evita produto cartesiano; cuidado: sem snapshot consistente entre as queries.
- Lookup em lote: `Where(f => ids.Contains(f.Id))` → `= ANY($1)`, uma query.

**No TruckFlow:** `AgendamentoRepositorio.GetAdminViewAsync` já projeta direto (bom para performance — dívida de camada é outra discussão); `GradeRepositorio.GetPagedAsync` carrega entidades e o service faz `MapToResponse` (ok para tabela pequena, revisar se crescer).

Referência: [EF Core — Efficient querying](https://learn.microsoft.com/ef/core/performance/efficient-querying) · [EF Core — Single vs split queries](https://learn.microsoft.com/ef/core/querying/single-split-queries)

---

### 7.4 Paginação offset (PagedResponse) para telas; keyset para feeds/exports grandes

**Impacto: MEDIUM-HIGH (keyset mantém O(page) em páginas profundas; offset degrada linearmente)**  
Tags: pagination, offset, keyset, cursor, count  
Arquivo: `rules/data-pagination.md`

Convenção do TruckFlow: **toda listagem de tela é paginada server-side** (`*ListQueryDto` + `PagedResponse<T>` com `TotalCount`/`TotalPages`). Isso usa `OFFSET` + `COUNT(*)` — ótimo para UI com número de página. Mas `OFFSET 50000` lê e descarta 50k linhas, e `COUNT(*)` varre tudo que bate no filtro.

**Incorreto (offset para export/feed grande, sem ordenação estável):**

```csharp
// export CSV de 1000 em 1000 com offset → cada página fica mais lenta
for (var page = 1; ; page++)
{
    var lote = await q.Skip((page - 1) * 1000).Take(1000).ToListAsync(ct); // sem OrderBy → páginas inconsistentes
}
```

**Correto — tela (padrão atual, com ordenação determinística):**

```csharp
var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
var pageSize = Math.Clamp(query.PageSize, 1, 100);                  // limite superior!
var totalCount = await dbQuery.CountAsync(ct);
var items = await dbQuery
    .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)          // desempate estável
    .Skip((pageNumber - 1) * pageSize).Take(pageSize)
    .Select(...).ToListAsync(ct);
```

**Correto — keyset (feed infinito, export, sincronização mobile):**

```sql
SELECT * FROM "Notificacao"
WHERE "EmpresaId" = $1 AND "DestinatarioUsuarioId" = $2
  AND ("CreatedAt", "Id") < ($3, $4)                 -- cursor da última linha vista
ORDER BY "CreatedAt" DESC, "Id" DESC
LIMIT 20;
-- usa o índice (EmpresaId, DestinatarioUsuarioId, CreatedAt) direto, em qualquer profundidade
```

```csharp
q = q.Where(n => n.CreatedAt < cursor.CreatedAt
              || (n.CreatedAt == cursor.CreatedAt && n.Id.CompareTo(cursor.Id) < 0))
     .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id).Take(20);
```

Sinais para trocar para keyset: export (`pageSize: 1000` no front), lista de notificações do mobile com scroll infinito, tabelas com >100k linhas por tenant. Sempre limite `PageSize` no backend (o front já pediu 1000 em export).

**No TruckFlow:** padrão em `GradeRepositorio.GetPagedAsync`, `PlanejamentoListQueryDto` + `PlanejamentoRecebimentoService.GetPaged`. Export de agendamentos no front (`VisualizarAgendamentoView`, `pageSize: 1000`).

Referência: [Use The Index, Luke — No Offset](https://use-the-index-luke.com/no-offset) · [Markus Winand — Paging Through Results](https://use-the-index-luke.com/sql/partial-results/fetch-next-page) · [EF Core — Pagination](https://learn.microsoft.com/ef/core/querying/pagination)

---

### 7.5 Leitura que não vai salvar usa AsNoTracking (ou projeção)

**Impacto: MEDIUM (30-50% menos CPU/memória em listagens; sem identity map inflado)**  
Tags: asnotracking, change-tracking, ef-core, read-only  
Arquivo: `rules/data-asnotracking.md`

Com tracking, o EF guarda snapshot de cada entidade para detectar mudanças no `SaveChanges`. Em listagem de 100 agendamentos com Includes, isso é memória e CPU desperdiçadas — e o risco de um `SaveChanges` posterior no mesmo contexto gravar algo que ninguém pretendia.

**Incorreto:**

```csharp
public async Task<PagedResponse<Grade>> GetPagedAsync(GradeListQueryDto q, CancellationToken ct)
{
    var items = await _db.Grade.Include(g => g.Produto)
        .OrderByDescending(x => x.CreatedAt).Skip(...).Take(...)
        .ToListAsync(ct);                       // tracked, só para mapear em DTO
}
```

**Correto:**

```csharp
var items = await _db.Grade.AsNoTracking()
    .Include(g => g.Produto)
    .OrderByDescending(x => x.CreatedAt).Skip(...).Take(...)
    .ToListAsync(ct);
// ou, melhor ainda, .Select(g => new GradeResponse { ... }) — projeção nunca é rastreada
```

`AsNoTracking` não muda o SQL; o ganho é no cliente. Os **filtros globais continuam valendo** (tenant protegido). `AsNoTrackingWithIdentityResolution()` quando o grafo tem a mesma entidade repetida. Não ligue `QueryTrackingBehavior.NoTracking` global: services de escrita do TruckFlow carregam e salvam no mesmo fluxo (`GetById` → método de domínio → `Update`).

**No TruckFlow:** repositórios de leitura paginada (`GradeRepositorio`, `AuditLogRepositorio`, `NotificacaoRepositorio`) são os candidatos; `GetById` usado antes de mutação deve continuar tracked.

Referência: [EF Core — Tracking vs No-Tracking](https://learn.microsoft.com/ef/core/querying/tracking)

---

### 7.6 Hot path de insert em lote — batch do EF ou binary COPY do Npgsql

**Impacto: MEDIUM (10-50x mais throughput que insert por linha com SaveChanges)**  
Tags: batch-insert, copy, npgsql, bulk, tracking  
Arquivo: `rules/data-batch-inserts.md`

Inserir pings de posição um a um (`Add` + `SaveChanges` por ping) custa round-trip + transação + tracking por linha. O EF Core já agrupa vários INSERTs num batch por `SaveChanges`; para volume alto e contínuo, `COPY` binário do Npgsql é a via mais rápida.

**Incorreto:**

```csharp
foreach (var p in dto.Posicoes)
{
    _db.MotoristaPosicaoHistorico.Add(Map(p));
    await _db.SaveChangesAsync(ct);          // 1 round-trip + 1 transação por ponto
}
```

**Correto (lote da requisição numa operação):**

```csharp
// Opção A: EF batch (simples; interceptors rodam)
_db.MotoristaPosicaoHistorico.AddRange(dto.Posicoes.Select(Map));
await _db.SaveChangesAsync(ct);              // INSERT multi-row em batches (MaxBatchSize default 42)

// Opção B: COPY binário (hot path ADR-0003; sem tracking, sem interceptors → EmpresaId explícito)
await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
await conn.OpenAsync(ct);
await using var writer = await conn.BeginBinaryImportAsync(
    """COPY "MotoristaPosicaoHistorico" ("MotoristaId","AgendamentoId","EmpresaId","Latitude","Longitude","Accuracy","Velocidade","CapturadoEm") FROM STDIN (FORMAT BINARY)""", ct);
foreach (var p in dto.Posicoes)
{
    await writer.StartRowAsync(ct);
    await writer.WriteAsync(motoristaId, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(dto.AgendamentoId, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(empresaId, NpgsqlDbType.Uuid, ct);      // do contexto, nunca do body
    // ... demais colunas
    await writer.WriteAsync(p.CapturadoEm.ToUniversalTime(), NpgsqlDbType.TimestampTz, ct);
}
await writer.CompleteAsync(ct);
```

```sql
INSERT INTO "MotoristaPosicaoHistorico" (...) VALUES ($1,...), ($9,...), ($17,...);
```

COPY pula o `EmpresaScopedSaveChangesInterceptor` — o service valida tenant/motorista/agendamento antes e escreve `EmpresaId` explicitamente. Combine com upsert da posição atual (`data-upsert`).

**No TruckFlow:** ADR-0003 pede "não usar EF tracking — é hot path" para `POST /v1/motorista/posicao`; ~14 req/s × batch.

Referência: [Npgsql — COPY](https://www.npgsql.org/doc/copy.html) · [EF Core — Batching](https://learn.microsoft.com/ef/core/performance/efficient-updating#batching) · [Populating a Database](https://www.postgresql.org/docs/16/populate.html)

---

### 7.7 Upsert atômico com INSERT ... ON CONFLICT, não SELECT-then-INSERT

**Impacto: MEDIUM (elimina corrida e duplicate key sob concorrência; 1 round-trip)**  
Tags: upsert, on-conflict, concurrency, idempotency  
Arquivo: `rules/data-upsert.md`

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

---

## Monitoramento & diagnóstico

**Impacto:** LOW-MEDIUM  
`EXPLAIN (ANALYZE, BUFFERS)`, `ToQueryString()`, `LogTo`, `pg_stat_statements`, VACUUM/ANALYZE.

### 8.1 Diagnostique com ToQueryString/LogTo + EXPLAIN (ANALYZE, BUFFERS) — não adivinhe

**Impacto: LOW-MEDIUM (acha a causa real (seq scan, sort em disco, estimativa errada) em minutos)**  
Tags: explain, explain-analyze, ef-core, toquerystring, diagnostics  
Arquivo: `rules/monitor-explain-analyze.md`

O LINQ que você escreveu não é o SQL que roda: o filtro global adiciona `"EmpresaId"`, `Include` vira JOIN, `Contains` vira `= ANY`. Primeiro pegue o SQL real; depois o plano real.

**Incorreto:**

```csharp
// "deve ser falta de índice" → cria índice aleatório e sobe
builder.HasIndex(x => x.UpdatedAt);
```

**Correto (1: SQL gerado):**

```csharp
var q = _db.Agendamento.Where(a => a.StatusAgendamento == StatusAgendamento.Agendado).OrderBy(a => a.DataInicio);
Console.WriteLine(q.ToQueryString());   // SQL com parâmetros (inclui o filtro de tenant)

// dev: log de todo SQL com tempo
options.UseNpgsql(cs).LogTo(Console.WriteLine, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
       .EnableSensitiveDataLogging();   // NUNCA em produção (dados pessoais/fiscais nos logs)
```

**Correto (2: plano real no psql com os mesmos parâmetros):**

```sql
EXPLAIN (ANALYZE, BUFFERS, VERBOSE, SETTINGS)
SELECT a.* FROM "Agendamento" a
WHERE a."EmpresaId" = '...' AND a."StatusAgendamento" = 2
ORDER BY a."DataInicio";
```

O que procurar:

| No plano | Significa | Ação |
|---|---|---|
| `Seq Scan` com `Rows Removed by Filter` alto | sem índice útil | `query-composite-indexes` |
| `rows=10` estimado vs `rows=50000` real | estatística velha / correlação | `ANALYZE`; `CREATE STATISTICS` |
| `Sort Method: external merge Disk` | `work_mem` baixo ou sort evitável | índice na ordem do `ORDER BY` |
| `Heap Fetches` alto em Index Only Scan | visibility map desatualizado | VACUUM (`monitor-vacuum-analyze`) |
| `Nested Loop` com loops=10000 | N+1 dentro do banco | reescrever JOIN / índice no lado interno |

`EXPLAIN ANALYZE` **executa** a query — em `UPDATE/DELETE`, embrulhe em `BEGIN; ... ROLLBACK;`. Cole planos complexos em explain.depesz.com ou explain.dalibo.com.

**No TruckFlow:** `TruckFlow.Benchmarks` (BenchmarkDotNet) e `TruckFlow.LoadTests` (NBomber) existem para medir antes/depois.

Referência: [Using EXPLAIN](https://www.postgresql.org/docs/16/using-explain.html) · [EF Core — Simple logging / ToQueryString](https://learn.microsoft.com/ef/core/logging-events-diagnostics/simple-logging) · [depesz — explain.depesz.com](https://explain.depesz.com/) · [pganalyze — EXPLAIN guide](https://pganalyze.com/docs/explain)

---

### 8.2 Use pg_stat_statements para achar as queries que realmente custam

**Impacto: LOW-MEDIUM (prioriza otimização pelo tempo total, não pela query mais "feia")**  
Tags: pg-stat-statements, monitoring, observability, performance  
Arquivo: `rules/monitor-pg-stat-statements.md`

A query que mais pesa raramente é a mais lenta — é a de 5ms chamada 2 milhões de vezes (polling de `unread-count`, `refetchInterval` de agendamentos). `pg_stat_statements` agrega por formato de query.

**Incorreto:**

```text
"Achei uma query de 800ms no log, vou otimizar essa" — roda 3x por dia.
```

**Correto:**

```sql
CREATE EXTENSION IF NOT EXISTS pg_stat_statements;  -- requer shared_preload_libraries (já vem em managed)

SELECT calls,
       round(total_exec_time::numeric, 0)           AS total_ms,
       round(mean_exec_time::numeric, 2)            AS media_ms,
       round(100 * total_exec_time / sum(total_exec_time) OVER (), 1) AS pct,
       rows,
       left(query, 120)                             AS query
FROM pg_stat_statements
ORDER BY total_exec_time DESC
LIMIT 20;

-- depois de otimizar e fazer deploy, zere para medir de novo
SELECT pg_stat_statements_reset();
```

Correlacione com o código: use `TagWith` no LINQ para o comentário aparecer no SQL e nos logs do servidor.

```csharp
var q = _db.Notificacao.TagWith("NotificacaoService.ContarNaoLidas")
    .CountAsync(n => n.DestinatarioUsuarioId == userId && n.LidaEm == null, ct);
```

**No TruckFlow:** ADR-0004 item 9 (OpenTelemetry → Prometheus/Grafana) — exporte métricas do `pg_stat_statements` (postgres_exporter) e a instrumentação `OpenTelemetry.Instrumentation.EntityFrameworkCore`/Npgsql para ter latência por query por tenant.

Referência: [pg_stat_statements](https://www.postgresql.org/docs/16/pgstatstatements.html) · [EF Core — Query tags](https://learn.microsoft.com/ef/core/querying/tags) · [Npgsql — OpenTelemetry tracing](https://www.npgsql.org/doc/diagnostics/tracing.html)

---

### 8.3 Ajuste autovacuum nas tabelas de fila e alto churn

**Impacto: LOW-MEDIUM (evita bloat, planos ruins e index-only scan degradado)**  
Tags: vacuum, autovacuum, analyze, bloat, queue-tables  
Arquivo: `rules/monitor-vacuum-analyze.md`

Tabelas de fila (`"OutboxEvent"`, `"NotificacaoEntrega"`) sofrem UPDATE em quase toda linha (pendente → processado) e acumulam tuplas mortas. Com o default (`autovacuum_vacuum_scale_factor = 0.2`), uma tabela de 5M linhas só é aspirada após 1M tuplas mortas — o índice parcial de pendentes incha e o claim do worker fica lento.

**Incorreto (defaults em tabela de fila grande):**

```sql
SELECT relname, n_live_tup, n_dead_tup, last_autovacuum, last_autoanalyze
FROM pg_stat_user_tables ORDER BY n_dead_tup DESC LIMIT 10;
-- "OutboxEvent" | 4.8M | 1.9M | 3 dias atrás
```

**Correto (thresholds por tabela):**

```sql
ALTER TABLE "OutboxEvent" SET (
    autovacuum_vacuum_scale_factor = 0.01,
    autovacuum_analyze_scale_factor = 0.02,
    autovacuum_vacuum_cost_limit = 1000
);
ALTER TABLE "NotificacaoEntrega" SET (autovacuum_vacuum_scale_factor = 0.02);
```

```csharp
// em migration
migrationBuilder.Sql("""ALTER TABLE "OutboxEvent" SET (autovacuum_vacuum_scale_factor = 0.01, autovacuum_analyze_scale_factor = 0.02);""");
```

Também: política de retenção para filas (apagar `"OutboxEvent"` processado > 30 dias em lotes, ou particionar por mês); `ANALYZE` manual depois de backfill grande; transações longas abertas (`conn-idle-timeout`) impedem o VACUUM de limpar qualquer coisa — é a causa mais comum de bloat "misterioso".

**No TruckFlow:** filas em `OutboxEventConfiguracao.cs`/`NotificacaoEntregaConfiguracao.cs`; `AuditLog` é append-only (cresce, não incha) — candidato a particionamento/retention, não a tuning de vacuum.

Referência: [Routine Vacuuming](https://www.postgresql.org/docs/16/routine-vacuuming.html) · [Cybertec — autovacuum tuning](https://www.cybertec-postgresql.com/en/tuning-autovacuum-postgresql/) · [Crunchy Data — Bloat](https://www.crunchydata.com/blog/postgres-performance-boost-hot-updates-and-fill-factor)

---

## Recursos avançados

**Impacto:** LOW  
LISTEN/NOTIFY para SSE (ADR-0002), indexação de `jsonb`, full-text search em português.

### 9.1 LISTEN/NOTIFY para fan-out do SSE — payload pequeno, conexão dedicada, fora do PgBouncer

**Impacto: MEDIUM (realtime entre N instâncias sem Redis; sem perder evento por payload grande)**  
Tags: listen-notify, pg-notify, sse, realtime, multi-instance  
Arquivo: `rules/advanced-listen-notify.md`

ADR-0002: o admin conectado na instância B precisa receber a notificação gravada pela instância A. `pg_notify` no commit + `LISTEN` em cada instância resolve sem broker externo. Mas NOTIFY tem limites que viram bug silencioso.

**Incorreto:**

```sql
-- payload com a notificação inteira (Corpo markdown, Payload jsonb...) → passa de 8000 bytes e falha
SELECT pg_notify('notif_realtime', row_to_json(n)::text) FROM "Notificacao" n WHERE ...;
```

```csharp
// LISTEN numa conexão do pool / atrás de PgBouncer transaction mode → para de receber sem erro
await using var conn = (NpgsqlConnection)_db.Database.GetDbConnection();
```

**Correto:**

```sql
-- payload mínimo: ids + tenant; o consumidor busca o resto se precisar
SELECT pg_notify('notif_realtime', json_build_object(
    'id', $1, 'empresaId', $2, 'destinatarioUsuarioId', $3)::text);
```

```csharp
// listener dedicado (BackgroundService), conexão própria e direta ao Postgres
await using var conn = new NpgsqlConnection(_listenConnectionString); // sem PgBouncer
await conn.OpenAsync(ct);
conn.Notification += (_, e) => _manager.Publicar(e.Payload);
await using (var cmd = new NpgsqlCommand("LISTEN notif_realtime", conn)) await cmd.ExecuteNonQueryAsync(ct);
while (!ct.IsCancellationRequested) await conn.WaitAsync(ct);    // reconectar com backoff ao cair
```

Propriedades a lembrar: NOTIFY dentro de transação só é entregue **no COMMIT** (rollback descarta — bom); payload máximo **8000 bytes**; entrega é *at-most-once* para quem está ouvindo — quem estava desconectado perde, por isso o front **invalida a query** ao receber e refaz o GET (fonte da verdade é a tabela, NOTIFY é só o "sininho"); fila de notificações tem limite (8GB) — listener lento trava `NOTIFY` dos produtores. Filtrar por `EmpresaId` antes de enviar ao cliente SSE.

**No TruckFlow:** `RealtimeNotificationInterceptor` (canal `notif_realtime`, `SELECT pg_notify(...)`), `RealtimeNotificationListener` (conexão dedicada, reconecta em 5s), `SseNotificationStreamer` (heartbeat 25s). Front: `useRealtimeNotifications.ts` invalida as queries de notificação.

Referência: [NOTIFY](https://www.postgresql.org/docs/16/sql-notify.html) · [LISTEN](https://www.postgresql.org/docs/16/sql-listen.html) · [Npgsql — Waiting for notifications](https://www.npgsql.org/doc/wait.html) · [Brandur — Notifier pattern](https://brandur.org/notifier)

---

### 9.2 Busca textual em português com tsvector + unaccent (ou trigram para nomes curtos)

**Impacto: LOW (100x mais rápido que ILIKE '%x%' em texto longo; acentos ignorados)**  
Tags: full-text-search, tsvector, portuguese, unaccent, trigram  
Arquivo: `rules/advanced-full-text-search.md`

O `search` das listagens com `ILIKE '%conceicao%'` não acha "Conceição" e faz seq scan. Para nomes curtos (fornecedor, produto, motorista), trigram + `unaccent` resolve (`query-index-types`). Para texto longo (observações, corpo de notificação, descrição de item de NF-e), full-text com dicionário `portuguese`.

**Incorreto:**

```csharp
q = q.Where(i => i.Descricao.ToLower().Contains(search.ToLower()));   // lower() na coluna: sem índice, sem acento
```

**Correto (coluna gerada + GIN):**

```sql
CREATE EXTENSION IF NOT EXISTS unaccent;
-- unaccent não é IMMUTABLE: wrapper para poder usar em coluna gerada/índice
CREATE FUNCTION immutable_unaccent(text) RETURNS text
    LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $$ SELECT public.unaccent('public.unaccent', $1) $$;

ALTER TABLE "NotaFiscalItem" ADD COLUMN "BuscaTsv" tsvector
    GENERATED ALWAYS AS (to_tsvector('portuguese', immutable_unaccent(coalesce("Descricao", '')))) STORED;
CREATE INDEX "IX_NotaFiscalItem_BuscaTsv" ON "NotaFiscalItem" USING gin ("BuscaTsv");

SELECT * FROM "NotaFiscalItem"
WHERE "EmpresaId" = $1 AND "BuscaTsv" @@ websearch_to_tsquery('portuguese', immutable_unaccent($2));
```

```csharp
// Npgsql EF: coluna NpgsqlTsVector + tradução de Matches
builder.Property(x => x.BuscaTsv).HasComputedColumnSql(
    "to_tsvector('portuguese', immutable_unaccent(coalesce(\"Descricao\", '')))", stored: true);
builder.HasIndex(x => x.BuscaTsv).HasMethod("gin");
q = q.Where(i => i.BuscaTsv.Matches(EF.Functions.WebSearchToTsQuery("portuguese", termo)));
```

Prefira trigram quando o usuário digita pedaço de palavra ("alim" → "Alimentos"); full-text quando digita palavras inteiras em texto longo.

**No TruckFlow:** `Search` em `GradeListQueryDto`, `UsuarioListQueryDto`, etc.; itens de NF-e (`NotaFiscalItem.Descricao`) e o `ProdutoLearningService` (sugestão de produto por descrição) são os candidatos.

Referência: [Full Text Search](https://www.postgresql.org/docs/16/textsearch.html) · [unaccent](https://www.postgresql.org/docs/16/unaccent.html) · [Npgsql — Full text search](https://www.npgsql.org/efcore/mapping/full-text-search.html)

---

### 9.3 Indexe jsonb só pelo que é consultado — GIN jsonb_path_ops ou índice de expressão

**Impacto: LOW (10-100x em filtros por chave de jsonb; evita GIN gigante desnecessário)**  
Tags: jsonb, gin, expression-index, payload  
Arquivo: `rules/advanced-jsonb-indexing.md`

`jsonb` sem índice em `WHERE` faz seq scan com parse por linha. Mas GIN genérico em todo `jsonb` custa escrita e espaço. Indexe o padrão de consulta real.

**Incorreto:**

```sql
-- "Notificacao"."Payload" (jsonb) consultado por agendamentoId sem índice
SELECT * FROM "Notificacao" WHERE "EmpresaId" = $1 AND "Payload" ->> 'agendamentoId' = $2;
-- ou GIN default em tudo "por via das dúvidas"
CREATE INDEX ON "Notificacao" USING gin ("Payload");
```

**Correto:**

```sql
-- uma chave específica, igualdade → índice de expressão B-tree (pequeno, rápido)
CREATE INDEX "IX_Notificacao_Payload_AgendamentoId"
    ON "Notificacao" ("EmpresaId", (("Payload" ->> 'agendamentoId')));

-- containment em várias chaves → GIN jsonb_path_ops (menor que o default)
CREATE INDEX "IX_Empresa_Configuracoes" ON "Empresa" USING gin ("Configuracoes" jsonb_path_ops);
SELECT "Id" FROM "Empresa" WHERE "Configuracoes" @> '{"AuthMethods":["EntraId"]}';
```

```csharp
// Expression index em EF: via migration
migrationBuilder.Sql("""CREATE INDEX CONCURRENTLY "IX_Notificacao_Payload_AgendamentoId" ON "Notificacao" ("EmpresaId", (("Payload" ->> 'agendamentoId')));""", suppressTransaction: true);
// GIN pelo Fluent API (Npgsql)
builder.HasIndex(x => x.Configuracoes).HasMethod("gin").HasOperators("jsonb_path_ops");
```

Se uma chave do jsonb é filtrada o tempo todo, ela provavelmente devia ser **coluna** (ex.: `AgendamentoId` na `Notificacao`). O endpoint `/notifications/agendamento/{id}` é sinal disso.

**No TruckFlow:** `Notificacao.Payload` (jsonb com IDs de domínio, ADR-0002), `Empresa.Configuracoes`.

Referência: [JSON Types — jsonb Indexing](https://www.postgresql.org/docs/16/datatype-json.html#JSON-INDEXING) · [Npgsql — JSON querying](https://www.npgsql.org/efcore/mapping/json.html)

---

## Referências

- https://www.postgresql.org/docs/16/
- https://www.npgsql.org/efcore/
- https://learn.microsoft.com/ef/core/
- https://use-the-index-luke.com/
- https://www.cybertec-postgresql.com/en/blog/
- https://www.crunchydata.com/blog
- https://www.depesz.com/
- https://pganalyze.com/blog
- https://hakibenita.com/
- https://brandur.org/
- https://www.laurenzalbe.com/
- https://github.com/supabase/agent-skills
