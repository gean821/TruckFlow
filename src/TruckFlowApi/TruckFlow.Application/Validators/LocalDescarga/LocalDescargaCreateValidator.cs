using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.LocalDescarga;

namespace TruckFlow.Application.Validators.LocalDescarga
{
    public class LocalDescargaCreateValidator : AbstractValidator<LocalDescargaCreateDto>
    {
        public LocalDescargaCreateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("Nome não pode ser vazio");
        }
    }
}
