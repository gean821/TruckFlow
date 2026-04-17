using System;
using FluentValidation;
using TruckFlow.Application.Validators.ItemPlanejamento;
using TruckFlow.Domain.Dto.Recebimento;

namespace TruckFlow.Application.Validators.Recebimento
{
    public class RecebimentoCreateDtoValidator : AbstractValidator<RecebimentoCreateDto>
    {
        public RecebimentoCreateDtoValidator()
        {
            RuleFor(x => x.FornecedorId)
              .NotEmpty().WithMessage("O campo FornecedorId é obrigatório.");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .Must(date => date.Date >= DateTime.UtcNow.Date)
                .WithMessage("A data de início não pode ser anterior à data atual.");

            RuleFor(x => x.DataFim)
                .NotEmpty().WithMessage("A data de fim é obrigatória.")
                .GreaterThanOrEqualTo(x => x.DataInicio)
                .WithMessage("A data de fim não pode ser anterior à data de início.");

            RuleFor(x => x.ItensPlanejamento)
                .NotNull().WithMessage("É necessário informar ao menos um item de planejamento.")
                .Must(x => x!.Count > 0).WithMessage("A lista de itens de planejamento não pode estar vazia.");

            RuleForEach(x => x.ItensPlanejamento)
                .SetValidator(new ItemPlanejamentoCreateDtoValidator());
        }
    }
}
