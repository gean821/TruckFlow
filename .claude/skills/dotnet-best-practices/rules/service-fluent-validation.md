---
title: Valide no service com FluentValidation e lance ValidationException
impact: HIGH
impactDescription: uma única porta de entrada para regras de payload, com mensagens PT-BR e formato VALIDATION_ERROR
tags: fluentvalidation, validation, service, dto
---

## Valide no service com FluentValidation e lance ValidationException

O projeto usa FluentValidation 12 **chamado manualmente** no início do método de service (sem auto-validation — o pacote de auto-validação do MVC está descontinuado pelo próprio FluentValidation). O validator é registrado por DTO na extension da feature. O middleware converte `ValidationException` em `400 { code: "VALIDATION_ERROR", errors: [...] }`.

Separação de responsabilidades:
- **Validator**: forma do payload (obrigatório, tamanho, formato de CNPJ/chave NF-e de 44 dígitos, `DataFim > DataInicio`). Sem I/O.
- **Service**: regras que precisam de banco (produto existe *no tenant*, doca livre) → `NotFoundException`/`BusinessException`.
- **Entidade**: invariantes de estado → `DomainException`.

**Incorreto:**

```csharp
public async Task<GradeResponse> CreateGrade(GradeCreateDto dto, CancellationToken ct)
{
    if (string.IsNullOrEmpty(dto.Nome)) throw new BusinessException("Nome obrigatório"); // espalhado
    // ...
}

// validator que faz I/O
RuleFor(x => x.ProdutoId).MustAsync(async (id, ct) => await _repo.Exists(id, ct));
```

**Correto (padrão `GradeService.CreateGrade`):**

```csharp
var validation = await _createValidator.ValidateAsync(dto, ct);
if (!validation.IsValid)
{
    _logger.LogWarning("Validação falhou ao criar grade: {Errors}",
        string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)));
    throw new ValidationException(validation.Errors);
}
var empresaId = _currentUser.GetEmpresaId();
var produto = await _produtoRepo.GetById(dto.ProdutoId, ct)
    ?? throw new NotFoundException("Produto não encontrado.");
```

```csharp
public sealed class TransportadoraCreateValidator : AbstractValidator<TransportadoraCreateDto>
{
    public TransportadoraCreateValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().WithMessage("Informe a razão social.")
            .MaximumLength(200).WithMessage("Razão social deve ter até 200 caracteres.");
        RuleFor(x => x.Cnpj).Matches(@"^\d{14}$").WithMessage("CNPJ deve ter 14 dígitos.");
    }
}

// Extensions/Transportadora/TransportadoraDependencyInjection.cs
services.AddScoped<IValidator<TransportadoraCreateDto>, TransportadoraCreateValidator>();
```

Namespace = pasta (`TruckFlow.Application.Validators.<Feature>`), classe `XCreateValidator` no arquivo `XCreateValidator.cs`.

**No TruckFlow:** `TruckFlow.Application/Validators/<Feature>/`. Dívidas: `AgendamentoAdminCreateValidator` em namespace `...AgendamentoMotorista`; arquivo `AgendamentoAdminUpdateValidator.cs` com classe `AgendamentoAdminUpdateDtoValidator`.

Referência: [FluentValidation — ASP.NET Core integration (manual validation recomendada)](https://docs.fluentvalidation.net/en/latest/aspnet.html) · [Vladimir Khorikov — Validation and DDD](https://enterprisecraftsmanship.com/posts/validation-and-ddd/)
