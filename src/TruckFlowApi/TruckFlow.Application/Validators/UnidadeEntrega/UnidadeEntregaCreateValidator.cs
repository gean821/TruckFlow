using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.UnidadeEntrega;

namespace TruckFlow.Application.Validators.UnidadeEntrega
{
    public class UnidadeEntregaCreateValidator : AbstractValidator<UnidadeEntregaCreateDto>
    {
        public UnidadeEntregaCreateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .MaximumLength(150);

            RuleFor(x => x.Localizacao)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Cep)
                .Matches(@"^\d{8}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Cep))
                .WithMessage("CEP deve conter 8 dígitos numéricos.");

            RuleFor(x => x.Estado)
                .Length(2)
                .When(x => !string.IsNullOrWhiteSpace(x.Estado))
                .WithMessage("Estado deve conter 2 caracteres.");
        }
    }
}
