using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Application.Validators.Fornecedor
{
    public class FornecedorCreateValidator : AbstractValidator<FornecedorCreateDto>
    {
        public FornecedorCreateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("O nome do fornecedor do produto não pode ser nulo.")
                .MinimumLength(2)
                .WithMessage("O nome precisa de ao menos 2 caracteres");

            RuleFor(x => x.Cnpj)
                .NotEmpty()
                .WithMessage("O CNPJ do fornecedor precisa ser informado para o seu cadastro.")
                .Length(14)
                .WithMessage("O CNPJ deve conter exatamente 14 dígitos numéricos.");

        }
    }
}
