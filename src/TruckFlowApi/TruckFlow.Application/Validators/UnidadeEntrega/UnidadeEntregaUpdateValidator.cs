using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.UnidadeEntrega;

namespace TruckFlow.Application.Validators.UnidadeEntrega
{
    public class UnidadeEntregaUpdateValidator : AbstractValidator<UnidadeEntregaUpdateDto>
    {
        public UnidadeEntregaUpdateValidator()
        {
            RuleFor(x => x.Nome)
                .MaximumLength(150)
                .When(x => x.Nome is not null);

            RuleFor(x => x.Localizacao)
                .MaximumLength(200)
                .When(x => x.Localizacao is not null);

            RuleFor(x => x.Cep)
                .Matches(@"^\d{8}$")
                .When(x => !string.IsNullOrWhiteSpace(x.Cep));

            RuleFor(x => x.Estado)
                .Length(2)
                .When(x => !string.IsNullOrWhiteSpace(x.Estado));
        }
    }
}
