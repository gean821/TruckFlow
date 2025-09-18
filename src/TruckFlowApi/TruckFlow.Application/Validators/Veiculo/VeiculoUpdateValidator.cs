using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Application.Dto.Veiculo;

namespace TruckFlow.Application.Validators.Veiculo
{
    public class VeiculoUpdateValidator : AbstractValidator<VeiculoUpdateDto>
    {
        public VeiculoUpdateValidator()
        {
            RuleFor(x => x.Placa)
                .NotEmpty()
                .WithMessage("A placa do veículo não pode ser nula")
                .Must(p => !string.IsNullOrWhiteSpace(p) && p.Trim().Length == 7)
                .WithMessage("A placa deve conter exatamente 7 caracteres")
                .Must(p => System.Text.RegularExpressions.Regex.IsMatch(
                    p.Trim().ToUpper(), "^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$"))
                .WithMessage("A placa do veículo deve estar no formato válido (AAA1234 ou Mercosul)");

            RuleFor(x => x.TipoVeiculo)
                .NotEmpty()
                .WithMessage("Deve ser selecionado o tipo do veículo.");
        }
    }
}
