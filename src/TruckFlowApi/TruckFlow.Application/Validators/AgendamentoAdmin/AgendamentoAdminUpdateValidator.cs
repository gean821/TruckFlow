using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Application.Validators.AgendamentoMotorista
{
    public sealed class AgendamentoAdminUpdateDtoValidator : AbstractValidator<AgendamentoAdminUpdateDto>
    {
        public AgendamentoAdminUpdateDtoValidator()
        {
            RuleFor(x => x.FornecedorId)
                  .NotEmpty().WithMessage("Selecione o fornecedor.");

            RuleFor(x => x.UnidadeEntregaId)
                .NotEmpty().WithMessage("Selecione a unidade de descarga (doca).");

            RuleFor(x => x.DataInicio)
                .NotEmpty().WithMessage("A data de início é obrigatória.")
                .GreaterThan(DateTime.Now).WithMessage("A data deve ser futura.");

            RuleFor(x => x.DataFim)
                .NotEmpty().WithMessage("A data de fim é obrigatória.")
                .GreaterThan(x => x.DataInicio).WithMessage("A data de fim deve ser posterior à data de início.");


            When(x => x.MotoristaId.HasValue, () =>
            {
            });
        }
    }
}

