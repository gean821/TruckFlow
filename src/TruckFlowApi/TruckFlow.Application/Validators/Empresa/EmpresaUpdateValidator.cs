using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Empresa;

namespace TruckFlow.Application.Validators.Empresa
{
    public class EmpresaUpdateValidator : AbstractValidator<EmpresaUpdateDto>
    {
        public EmpresaUpdateValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress()
                .When(x => x.Email != null);

            RuleFor(x => x.Estado)
                .Length(2)
                .When(x => x.Estado != null);
        }
    }

}
