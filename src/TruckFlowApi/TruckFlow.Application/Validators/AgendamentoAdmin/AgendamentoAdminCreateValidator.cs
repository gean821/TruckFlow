using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Application.Validators.AgendamentoMotorista
{
    public sealed class AgendamentoAdminCreateValidator : AbstractValidator<AgendamentoAdminCreateDto>
    {
        public AgendamentoAdminCreateValidator()
        {

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .GreaterThan(DateTime.Now.AddMinutes(-5)).WithMessage("A data informada já passou.");

            RuleFor(x => x.DataFim)
                .NotEmpty().WithMessage("A data de fim é obrigatória.")
                .GreaterThan(x => x.DataInicio).WithMessage("A data de fim deve ser posterior à data de início.");

            RuleFor(x => x.LocalDescargaId)
                 .NotEmpty().WithMessage("Selecione a doca/local de descarga.");

            RuleFor(x => x.ProdutoId)
                .NotEmpty().WithMessage("O agendamento precisa de no mínimo um produto.");
            
        }
    }
}
