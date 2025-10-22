using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.NotaFiscal;

namespace TruckFlow.Application.Validators.NotaFiscal
{
    public sealed class NotaFiscalItemDtoValidator : AbstractValidator<NotaFiscalItemDto>
    {
        public NotaFiscalItemDtoValidator()
        {
            RuleFor(x => x.Codigo)
               .NotEmpty().WithMessage("O código do produto é obrigatório.")
               .MaximumLength(60).WithMessage("O código do produto deve ter no máximo 60 caracteres.");

            RuleFor(x => x.Descricao)
                .NotEmpty().WithMessage("A descrição do produto é obrigatória.")
                .MinimumLength(3).WithMessage("A descrição do produto deve ter pelo menos 3 caracteres.");

            RuleFor(x => x.Quantidade)
                .GreaterThan(0).WithMessage("A quantidade do produto deve ser maior que zero.");

            RuleFor(x => x.Unidade)
                .NotEmpty().WithMessage("A unidade do produto é obrigatória.")
                .MaximumLength(6).WithMessage("A unidade do produto deve ter no máximo 6 caracteres.");

            RuleFor(x => x.ValorUnitario)
                .GreaterThan(0).WithMessage("O valor unitário deve ser maior que zero.");

            RuleFor(x => x.ValorTotal)
                .GreaterThan(0).WithMessage("O valor total deve ser maior que zero.")
                .Equal(x => x.Quantidade * x.ValorUnitario)
                .WithMessage("O valor total deve ser igual à quantidade x valor unitário.");
        }
    }
}

