using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Grade;
using TruckFlow.Domain.Entities;

namespace TruckFlow.Application.Validators.Grade
{
    public class GradeCreateValidator : AbstractValidator<GradeCreateDto>
    {
        public GradeCreateValidator()
        {
            RuleFor(x => x.ProdutoId)
                .NotEmpty()
                .WithMessage("O produto deve ser informado.");

            RuleFor(x => x.LocalDescargaId)
                .NotEmpty()
                .WithMessage("O local de descarga deve ser informado.");

            RuleFor(x => x.DataInicio)
                .NotNull()
                .WithMessage("A data de início deve ser informada.")
                .GreaterThanOrEqualTo(_ =>
                    DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.UtcNow, TruckFlow.Domain.Entities.Grade.OperationalTimeZone)))
                .WithMessage("A data de início não pode ser no passado.");

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
                .NotEqual(x => x.HoraInicial)
                .WithMessage("A hora final não pode ser igual à hora inicial.");

            RuleFor(x => x.IntervaloMinutos)
                .GreaterThan(0)
                .WithMessage("O intervalo em minutos deve ser maior que 0.");

            RuleFor(x => x)
                .Must(dto => GradeJanelaRules.IntervaloCabeNaJanela(dto.HoraInicial, dto.HoraFinal, dto.IntervaloMinutos))
                .WithName("IntervaloMinutos")
                .WithMessage("O intervalo em minutos não pode ser maior que a janela entre a hora inicial e a hora final.")
                .When(dto => dto.IntervaloMinutos > 0);

            RuleFor(x => x.DiasSemana)
    .NotEmpty().WithMessage("Dias da semana são obrigatórios.")
    .Must(ds => ds.Split(',')
        .All(d => int.TryParse(d, out var dia) && dia >= 0 && dia <= 6))
    .WithMessage("Dias da semana inválidos.");
        }
    }
}
