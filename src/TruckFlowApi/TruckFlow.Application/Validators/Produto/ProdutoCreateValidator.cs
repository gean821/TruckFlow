using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Produto;

namespace TruckFlow.Application.Validators.Produto
{
    public class ProdutoCreateValidator : AbstractValidator<ProdutoCreateDto>
    {
        public ProdutoCreateValidator()
        {
            RuleFor(x => x.Nome)
                .NotEmpty()
                .WithMessage("O nome do produto não pode ser nulo")
                .MinimumLength(3)
                .WithMessage("O nome precisa de ao menos 3 Caracteres");
        }
    }
}
