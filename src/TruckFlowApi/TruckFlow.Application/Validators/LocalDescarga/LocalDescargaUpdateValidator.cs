using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.LocalDescarga;

namespace TruckFlow.Application.Validators.LocalDescarga
{
    public class LocalDescargaUpdateValidator : AbstractValidator<LocalDescargaUpdateDto>
    {
        public LocalDescargaUpdateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("O Nome do local de descarga não pode ser vazio");
        }
    }
}
