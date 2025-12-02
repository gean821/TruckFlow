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
            RuleFor(x => x.FornecedorId)
                .NotEmpty()
                .WithMessage("O fornecedor deve ser informado.");

            RuleFor(x => x.ProdutoId)
                .NotEmpty()
                .WithMessage("O produto deve ser informado.");

            RuleFor(x => x.DataInicio)
                .NotNull()
                .WithMessage("A data de início deve ser informada.");

            RuleFor(x => x.DataFim)
                .NotNull()
                .WithMessage("A data de fim deve ser informada.")
                .GreaterThanOrEqualTo(x => x.DataInicio)
                .WithMessage("A data de fim não pode ser menor que a data de início.");

            RuleFor(x => x.HoraInicial)
                .NotNull()
                .WithMessage("A hora inicial deve ser informada.");

            RuleFor(x => x.HoraFinal)
                .NotNull()
                .WithMessage("A hora final deve ser informada.")
                .GreaterThan(x => x.HoraInicial)
                .WithMessage("A hora final deve ser maior que a hora inicial.");

            RuleFor(x => x.IntervaloMinutos)
                .GreaterThan(0)
                .WithMessage("O intervalo em minutos deve ser maior que 0.");
        }
    }
}
