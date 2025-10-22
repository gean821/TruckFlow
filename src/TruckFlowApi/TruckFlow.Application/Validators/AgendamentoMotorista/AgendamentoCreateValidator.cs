using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Agendamento;

namespace TruckFlow.Application.Validators.AgendamentoMotorista
{
    public sealed class AgendamentoCreateValidator : AbstractValidator<AgendamentoCreateDto>
    {
        public AgendamentoCreateValidator()
        {
            RuleFor(x => x.UnidadeDescarga)
                 .NotEmpty()
                 .WithMessage("A unidade de descarga é obrigatória.")
                 .MaximumLength(100).WithMessage("A unidade de descarga deve ter no máximo 100 caracteres.");

            RuleFor(x => x.Fornecedor)
                .NotEmpty().WithMessage("O fornecedor é obrigatório.")
                .MaximumLength(100)
                .WithMessage("O fornecedor deve ter no máximo 100 caracteres.");

            RuleFor(x => x.VolumeCarga)
                .NotEmpty()
                .WithMessage("O peso da carga é obrigatório.")
                .Matches(@"^\d+(\.\d{1,2})?$").WithMessage("O peso da carga deve ser um número válido (ex: 1500.50).");

            RuleFor(x => x.Produto)
                .NotEmpty()
                .WithMessage("O produto é obrigatório.")
                .MaximumLength(100).WithMessage("O nome do produto deve ter no máximo 100 caracteres.");
        }
    }
}
