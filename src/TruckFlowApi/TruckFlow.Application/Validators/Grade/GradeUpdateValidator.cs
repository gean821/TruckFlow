using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Grade;

namespace TruckFlow.Application.Validators.Grade
{
    public class GradeUpdateValidator : AbstractValidator<GradeUpdateDto>
    {
        public GradeUpdateValidator()
        {
            // Validator de update só é acionado quando o campo é enviado pelo front
            RuleFor(x => x.ProdutoId)
                .NotEmpty()
                .WithMessage("O produto deve ser informado.")
                .When(x => x.ProdutoId.HasValue);

            RuleFor(x => x.DataFim)
                .GreaterThanOrEqualTo(x => x.DataInicio)
                .WithMessage("A data de fim não pode ser menor que a data de início.")
                .When(x => x.DataFim.HasValue && x.DataInicio.HasValue);

            RuleFor(x => x.HoraFinal)
                .GreaterThan(x => x.HoraInicial)
                .WithMessage("A hora final deve ser maior que a hora inicial.")
                .When(x => x.HoraFinal.HasValue && x.HoraInicial.HasValue);

            RuleFor(x => x.IntervaloMinutos)
                .GreaterThan(0)
                .WithMessage("O intervalo em minutos deve ser maior que 0.")
                .When(x => x.IntervaloMinutos.HasValue);
        }
    }
}
