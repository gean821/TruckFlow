# Diretrizes de escrita das regras

Regras para agentes (e humanos) aplicarem sem ambiguidade.

## Princípios

### 1. Transformação concreta
Mostre a reescrita exata. "Troque `.Include(...).ToListAsync()` por `.Select(x => new XResponse {...})`" — não "otimize suas queries".

### 2. Erro primeiro
Sempre o anti-padrão antes da solução. Isso treina o agente a **reconhecer** o problema em código existente.

### 3. Impacto quantificado
"Index scan em vez de seq scan em 2M linhas: 800ms → 3ms", "elimina 1+N queries". Nada de "mais rápido".

### 4. SQL **e** EF Core
O time escreve LINQ e Fluent API; o banco executa SQL. Toda regra mostra os dois lados. Para SQL cru em C#, use `FromSqlInterpolated`/`ExecuteSqlInterpolatedAsync` (parametrizado) — nunca concatenação de string.

### 5. Identificadores do TruckFlow
As tabelas são `ToTable(nameof(Entity))`: **PascalCase singular entre aspas** (`"Agendamento"`, `"NotificacaoEntrega"`, colunas `"EmpresaId"`). Use os nomes reais nos exemplos — carregam intenção e dá para colar no `psql`.

### 6. Âncora no código real
Toda regra tem uma seção **No TruckFlow:** apontando arquivo, migration ou ADR. Se a regra contraria código existente, diga explicitamente ("dívida conhecida, não copie").

### 7. Multi-tenant por padrão
Exemplos de query em tabela `IEmpresaScoped` incluem `"EmpresaId" = @empresaId` no SQL (o filtro global injeta isso no LINQ). Índice de exemplo em tabela de tenant começa por `"EmpresaId"` salvo motivo explícito.

## Checklist de revisão de uma regra

- [ ] Frontmatter completo (title, impact, impactDescription, tags)
- [ ] Incorreto + Correto em SQL e C#
- [ ] Exemplos rodam (ou quase) contra o schema real
- [ ] Seção "No TruckFlow"
- [ ] Referência a doc oficial ou autor reconhecido
- [ ] `AGENTS.md` recompilado
