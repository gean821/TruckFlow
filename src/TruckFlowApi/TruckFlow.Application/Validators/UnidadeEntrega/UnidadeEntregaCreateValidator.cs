using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.UnidadeEntrega;

namespace TruckFlow.Application.Validators.UnidadeEntrega
{
    public class UnidadeEntregaCreateValidator : AbstractValidator<UnidadeEntregaCreateDto>
    {
        public UnidadeEntregaCreateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("Nome não pode ser vazio");
        }
    }
}
