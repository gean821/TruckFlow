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
            RuleFor(x => x.FornecedorId)
                 .NotEmpty().WithMessage("Selecione o fornecedor.");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .GreaterThan(DateTime.Now.AddMinutes(-5)).WithMessage("A data informada já passou.");

            RuleFor(x => x.LocalDescargaId)
                 .NotEmpty().WithMessage("Selecione a doca/local de descarga.");
        }
    }
}
