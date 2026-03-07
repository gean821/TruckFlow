using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Empresa;

namespace TruckFlow.Application.Validators.Empresa
{
    public class EmpresaCreateValidator : AbstractValidator<EmpresaCreateDto>
    {
        public EmpresaCreateValidator()
        {
            RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(200);
            RuleFor(x => x.NomeFantasia).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Cnpj).NotEmpty().Length(14);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Telefone).NotEmpty();
            RuleFor(x => x.Cep).NotEmpty();
            RuleFor(x => x.Logradouro).NotEmpty();
            RuleFor(x => x.Numero).NotEmpty();
            RuleFor(x => x.Bairro).NotEmpty();
            RuleFor(x => x.Cidade).NotEmpty();
            RuleFor(x => x.Estado).NotEmpty().Length(2);
        }
    }
}
