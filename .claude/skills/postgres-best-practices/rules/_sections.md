# Seções

Define as categorias, ordem e impacto. O ID da seção (entre parênteses) é o prefixo do arquivo da regra.

---

## 1. Performance de query (query)
**Impact:** CRITICAL
**Description:** Índice faltando ou errado, planos ruins. A causa nº 1 de lentidão — e no TruckFlow quase toda query é filtrada por `EmpresaId` antes de qualquer outra coisa.

## 2. Gestão de conexões (conn)
**Impact:** CRITICAL
**Description:** Pool do Npgsql, limites do servidor, PgBouncer (ADR-0004 item 8) e o que quebra em modo transaction (prepared statements, LISTEN).

## 3. Segurança & multi-tenant (security)
**Impact:** CRITICAL
**Description:** Isolamento entre empresas via `IEmpresaScoped` + `HasQueryFilter` + `EmpresaScopedSaveChangesInterceptor`, escapes explícitos `*AcrossTenants`, RLS como defesa em profundidade e privilégios mínimos.

## 4. Migrations seguras (migration)
**Impact:** CRITICAL
**Description:** Mudança de schema sem downtime em tabela quente, com mobile já publicado consumindo o contrato e 2+ instâncias subindo. Fora do startup (ADR-0004 item 2).

## 5. Design de schema (schema)
**Impact:** HIGH
**Description:** Tipos (`uuid`, `timestamptz`, `numeric`, `jsonb`), chaves, constraints que garantem regra de negócio (exclusion constraint), particionamento para retenção LGPD.

## 6. Concorrência & locking (lock)
**Impact:** MEDIUM-HIGH
**Description:** Concorrência otimista com `xmin`, transações curtas, `FOR UPDATE SKIP LOCKED` para workers em multi-instância, advisory locks, prevenção de deadlock.

## 7. Padrões de acesso a dados (data)
**Impact:** MEDIUM
**Description:** N+1, tracking, projeção, bulk (`ExecuteUpdate/Delete` pula interceptors!), paginação offset vs keyset, upsert, batch, idempotência.

## 8. Monitoramento & diagnóstico (monitor)
**Impact:** LOW-MEDIUM
**Description:** `EXPLAIN (ANALYZE, BUFFERS)`, `ToQueryString()`, `LogTo`, `pg_stat_statements`, VACUUM/ANALYZE.

## 9. Recursos avançados (advanced)
**Impact:** LOW
**Description:** LISTEN/NOTIFY para SSE (ADR-0002), indexação de `jsonb`, full-text search em português.
