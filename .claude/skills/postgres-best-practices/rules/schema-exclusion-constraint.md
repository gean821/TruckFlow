---
title: Garanta "sem sobreposição de horário por doca" com exclusion constraint, não com SELECT antes do INSERT
impact: CRITICAL
impactDescription: impossível dupla reserva da mesma doca, mesmo com N instâncias concorrentes
tags: exclusion-constraint, btree-gist, tstzrange, concurrency, integrity
---

## Garanta "sem sobreposição de horário por doca" com exclusion constraint, não com SELECT antes do INSERT

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
