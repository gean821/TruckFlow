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
                .NotEmpty()
                .WithMessage("Nome não pode ser vazio");
        }
    }
}
