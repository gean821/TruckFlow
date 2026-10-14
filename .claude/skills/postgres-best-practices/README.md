# PostgreSQL Best Practices (TruckFlow) — Guia do contribuidor

Regras de PostgreSQL + EF Core/Npgsql otimizadas para agentes (Claude Code) e para revisão humana. Adaptado de `supabase/postgres-best-practices` (MIT).

## Estrutura

```
.claude/skills/postgres-best-practices/
├── SKILL.md           # Manifesto da skill (o que o agente lê primeiro)
├── AGENTS.md          # [GERADO] Documento compilado com todas as regras
├── README.md          # Este arquivo
├── build_agents.py    # Compila rules/*.md → AGENTS.md
├── metadata.json      # Versão, resumo, referências
└── rules/
    ├── _template.md      # Modelo de regra
    ├── _sections.md      # Seções e impacto (o prefixo do arquivo define a seção)
    ├── _contributing.md  # Diretrizes de escrita
    └── <prefixo>-*.md    # Regras individuais
```

## Criando uma regra

1. Escolha o prefixo: `query-`, `conn-`, `security-`, `migration-`, `schema-`, `lock-`, `data-`, `monitor-`, `advanced-`.
2. Copie `rules/_template.md` para `rules/<prefixo>-<nome>.md` e preencha.
3. Mostre **SQL e EF Core**. Se a regra nasce de um incidente/bug do TruckFlow, cite arquivo e migration.
4. Recompile: `python .claude/skills/postgres-best-practices/build_agents.py`

## Estrutura de uma regra

- Frontmatter: `title`, `impact` (CRITICAL | HIGH | MEDIUM-HIGH | MEDIUM | LOW-MEDIUM | LOW), `impactDescription` (ganho quantificado), `tags`
- `## Título` + explicação de 1-3 frases
- **Incorreto** — SQL e C#
- **Correto** — SQL e C#
- **No TruckFlow:** onde isso aparece no código real
- `Referência:` link para doc oficial ou autor reconhecido

## Níveis de impacto

| Nível | Ganho | Exemplos |
|---|---|---|
| CRITICAL | 10-100x ou incidente evitado | índice faltando, vazamento entre tenants, migration travando tabela |
| HIGH | 5-20x | tipo de índice errado, particionamento |
| MEDIUM-HIGH | 2-5x | N+1, contenção de lock |
| MEDIUM | 1.5-3x | tracking desnecessário, estatística velha |
| LOW-MEDIUM | 1.2-2x | tuning de VACUUM |
| LOW | Incremental | casos específicos |
