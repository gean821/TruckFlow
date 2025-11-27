using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckFlow.Domain.Dto.Recebimento;

namespace TruckFlow.Application.Validators.Recebimento
{
    public class RecebimentoUpdateDtoValidator : AbstractValidator<RecebimentoUpdateDto>
    {
        public RecebimentoUpdateDtoValidator()
        {

        }
    }
}
