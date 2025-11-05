using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.ItensPlanejamento;

namespace TruckFlow.Application.Validators.ItemPlanejamento
{
    public class ItemPlanejamentoCreateDtoValidator : AbstractValidator<ItemPlanejamentoCreateDto>
    {
        public ItemPlanejamentoCreateDtoValidator()
        {
            RuleFor(x => x.ProdutoId)
                    .NotEmpty().WithMessage("O campo ProdutoId é obrigatório.");

            RuleFor(x => x.QuantidadeTotalPlanejada)
                .GreaterThan(0).WithMessage("A quantidade total planejada deve ser maior que zero.");

            RuleFor(x => x.CadenciaDiariaPlanejada)
                .GreaterThan(0).WithMessage("A cadência diária planejada deve ser maior que zero.")
                .Must((dto, cadencia) => cadencia <= dto.QuantidadeTotalPlanejada)
                .WithMessage("A cadência diária planejada não pode ser maior que a quantidade total planejada.");
        }
    }
}

