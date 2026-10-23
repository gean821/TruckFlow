# Seções

Este arquivo define as categorias de regra, a ordem, o impacto e a descrição.
O ID da seção (entre parênteses) é o prefixo do nome do arquivo usado para agrupar as regras.

---

## 1. Isolamento multi-tenant (tenant)

**Impacto:** CRITICAL
**Descrição:** As 21 fábricas Aurora são marcas concorrentes dentro da mesma cooperativa (ADR-0001). Vazamento entre `Empresa`s é vazamento competitivo real e é o item nº 1 do OWASP API Top 10 (BOLA). Nenhuma outra categoria compensa um erro aqui.

## 2. Erros e exceções (error)

**Impacto:** CRITICAL
**Descrição:** Admin Vue e app mobile leem `response.data.message` para mostrar ao operador. Exceção no tipo errado vira 500 genérico; exceção engolida vira dado inconsistente; stack trace em produção vira achado de pen-test.

## 3. Contrato HTTP e controllers (api)

**Impacto:** HIGH
**Descrição:** Controllers magros, rotas versionadas, status codes corretos, listagens paginadas e idempotência. É o contrato consumido por dois clientes (web + mobile publicado em loja), então mudança aqui é cara.

## 4. Services e casos de uso (service)

**Impacto:** HIGH
**Descrição:** Onde mora a orquestração: validação, resolução de tenant, transação, mapeamento para DTO e log. Services retornam DTOs, nunca entidades EF.

## 5. Modelo de domínio (domain)

**Impacto:** HIGH
**Descrição:** Invariantes e transições de estado dentro das entidades, eventos de domínio via outbox, factories, enums persistidos e tempo em UTC.

## 6. Acesso a dados com EF Core (data)

**Impacto:** HIGH
**Descrição:** Repositórios por agregado, nulabilidade honesta, leitura sem tracking, async de ponta a ponta e concorrência otimista com `xmin`. Para SQL/índices/locks profundos, ver skill `postgres-best-practices`.

## 7. Injeção de dependência e configuração (di)

**Impacto:** MEDIUM
**Descrição:** Uma extension por feature, lifetimes coerentes, HttpClient tipado com resiliência e Options validadas no startup.

## 8. Background services e concorrência entre instâncias (async)

**Impacto:** MEDIUM
**Descrição:** Workers que funcionam com 2+ instâncias da API (ADR-0004 item 1 e 4), escopo de DI por iteração, cancelamento e bibliotecas síncronas.

## 9. Testes (test)

**Impacto:** MEDIUM
**Descrição:** Integração com Postgres real (Testcontainers), sem mock de banco, gate multi-tenant e concorrência obrigatórios.

## 10. Segurança operacional (security)

**Impacto:** MEDIUM-HIGH
**Descrição:** Segredos fora do repositório, superfície anônima mínima, rate limiting e Swagger fechado em produção — itens que caem direto no questionário SAD da Aurora.
