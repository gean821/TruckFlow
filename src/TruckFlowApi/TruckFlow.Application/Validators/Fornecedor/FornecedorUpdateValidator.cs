using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Fornecedor;

namespace TruckFlow.Application.Validators.Fornecedor
{
    public class FornecedorUpdateValidator : AbstractValidator<FornecedorUpdateDto>
    {
        public FornecedorUpdateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("O nome do fornecedor não pode ser nulo.")
                .MinimumLength(2)
                .WithMessage("O nome precisa de ao menos 2 caracteres");

            //RuleFor(x => x.ProdutoId)
            //    .NotEmpty()
            //    .WithMessage("Deve ser selecionado ao menos 1 produto associado");
        }

    }
}
