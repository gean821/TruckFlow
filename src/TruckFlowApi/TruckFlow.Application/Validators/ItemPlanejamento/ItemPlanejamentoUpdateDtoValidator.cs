using System;
using System.Linq;
using FluentValidation;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Application.Validators.ItemPlanejamento
{
    public class ItemPlanejamentoUpdateDtoValidator : AbstractValidator<ItemPlanejamentoUpdateDto>
    {
        public ItemPlanejamentoUpdateDtoValidator()
        {
            RuleFor(x => x.ProdutoId)
                .NotEmpty().WithMessage("O campo ProdutoId é obrigatório.");

            RuleFor(x => x.CadenciaDiariaPlanejada)
                .GreaterThan(0).WithMessage("A meta diária deve ser maior que zero.");

            RuleFor(x => x.DiasSemana)
                .NotEmpty().WithMessage("Informe ao menos um dia da semana.")
                .Must(SerListaDeDiasValida).WithMessage("Dias da semana inválidos.");

            RuleFor(x => x.ToleranciaExtra)
                .GreaterThanOrEqualTo(0).When(x => x.ToleranciaExtra.HasValue)
                .WithMessage("A tolerância não pode ser negativa.");
        }

        private static bool SerListaDeDiasValida(string? ds)
        {
            if (string.IsNullOrWhiteSpace(ds)) return false;

            return ds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .All(p => int.TryParse(p, out var n) && n >= 0 && n <= 6);
        }
    }
}
