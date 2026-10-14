---
title: Traduza SqlState do Postgres e DbUpdateConcurrencyException em BusinessException
impact: CRITICAL
impactDescription: corrida de reserva vira mensagem acionável em vez de 500
tags: postgres, npgsql, concurrency, exclusion-constraint, unique, error-handling
---

## Traduza SqlState do Postgres e DbUpdateConcurrencyException em BusinessException

No TruckFlow a **última linha de defesa contra corrida é o banco** (xmin, exclusion constraint, unique filtrado). A checagem prévia na aplicação (`GetConflitosAsync`) dá mensagem amigável no caso comum, mas a garantia vem do Postgres — e o erro dele precisa virar `BusinessException` com texto útil. Sem isso, o motorista vê "Erro interno" quando na verdade alguém pegou a vaga 50ms antes.

| SqlState / exceção | Significado | Mensagem sugerida |
|---|---|---|
| `DbUpdateConcurrencyException` | `xmin` mudou (otimista) | "Registro foi alterado por outra pessoa. Atualize e tente de novo." |
| `23505` unique_violation | duplicado | "Já existe ... com esses dados." |
| `23P01` exclusion_violation | sobreposição de horário na doca | "Conflito de horário nesta doca." |
| `23503` foreign_key_violation | referência inexistente / em uso | "Não é possível excluir: ... está em uso." |
| `40001` serialization_failure | isolamento Serializable | retry com backoff (não exponha ao usuário) |

**Incorreto:**

```csharp
await _repo.Update(vaga, ct);   // estoura DbUpdateException → 500
```

```csharp
catch (Exception) { throw new BusinessException("Erro ao reservar."); } // engole a causa
```

**Correto (padrão de `AgendamentoMotoristaService.BookAppointment`):**

```csharp
await using var tx = await _repo.BeginTransactionAsync(ct);
try
{
    await _repo.Update(vaga, ct);
    await _lifecycle.AoReservarAsync(vaga, pesoEstimado, ct);
    await tx.CommitAsync(ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new BusinessException(
        "Vaga já foi reservada por outro motorista. Atualize a lista e tente outra.");
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
{
    throw new BusinessException("Conflito de horário detectado para esta doca.");
}
catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
{
    throw new BusinessException("Esta vaga já foi reservada por outro motorista neste instante.");
}
```

Prefira as constantes `PostgresErrorCodes.*` do Npgsql a strings mágicas. Se o padrão se repetir em mais serviços, extraia um helper (`PostgresErrors.IsUniqueViolation(ex, "nome_constraint")`) — checando o **nome da constraint** (`pg.ConstraintName`) quando a tabela tem mais de uma unique.

**No TruckFlow:** repetido em `AgendamentoAdminService` (Create/Update/Cancelar/Finalizar) e `GradeService.CreateGrade`. Lacuna: `DbUpdateConcurrencyException` não tratada no service vira 500 pelo middleware.

Referência: [Npgsql — PostgresErrorCodes](https://www.npgsql.org/doc/api/Npgsql.PostgresErrorCodes.html) · [PostgreSQL Error Codes](https://www.postgresql.org/docs/current/errcodes-appendix.html) · [EF Core — Handling concurrency conflicts](https://learn.microsoft.com/ef/core/saving/concurrency)
